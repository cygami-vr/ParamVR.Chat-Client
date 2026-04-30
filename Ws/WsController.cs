using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace ParamVR.Ws;

internal class WsController: IDisposable
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static WsController Instance { get; private set; } = new();

    public CancellationTokenSource CancelTokenSource { get; private set; } = new();
    public CancellationToken CancelToken => CancelTokenSource.Token;

    private Task? supervisorTask;
    private readonly object supervisorMutex = new();
    public event Action<Status>? StatusChanged;
    public ClientWebSocket? Socket { get; private set; }

    private WsController() {}

    public void UpdateStatus(Status status)
    {
        logger.Info("New status = {status}", status);
        StatusChanged?.Invoke(status);
    }

    public void StartSupervisor()
    {
        lock (supervisorMutex)
            if (supervisorTask == null || supervisorTask.IsCompleted)
                supervisorTask = SupervisorLoop();
    }

    private async Task SupervisorLoop()
    {
        logger.Trace("Supervisor loop started.");

        while (!CancelTokenSource.IsCancellationRequested)
        {
            try
            {
                await Connect(CancelTokenSource.Token);
                if (Socket != null)
                    await WsReceiver.StartReceiveLoop(Socket);
            }
            catch (OperationCanceledException)
            {
                logger.Trace("Supervisor cancelled.");
                break;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Connection attempt failed; retrying in 5s.");
                UpdateStatus(Status.FAILED_RETRYING);
                try
                {
                    await Task.Delay(5000, CancelTokenSource.Token);
                }
                catch (TaskCanceledException) {}
            }
        }

        logger.Trace("Supervisor loop exited.");
    }

    public async Task Restart()
    {
        logger.Info("Restart requested.");
        UpdateStatus(Status.CONNECTING);
        CancelTokenSource.Cancel();

        Task? oldSupervisorTask = null;
        lock (supervisorMutex)
            oldSupervisorTask = supervisorTask;

        if (oldSupervisorTask != null)
            await oldSupervisorTask;

        CancelTokenSource.Dispose();
        CancelTokenSource = new CancellationTokenSource();
        await Close();
        var settings = Settings.Instance.SettingsData;
        if (settings.targetUser?.Length > 0 && settings.listenKey?.Length > 0)
        {
            StartSupervisor();
        }
        else
        {
            logger.Info("Connection info missing.");
            UpdateStatus(Status.DISCONNECTED);
        }
    }

    public async Task Close()
    {
        if (Socket != null)
        {
            try
            {
                logger.Info("Closing websocket.");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cts.Token);
            }
            catch {}
            finally
            {
                Socket.Dispose();
                Socket = null;
            }
        }
    }

    private async Task Connect(CancellationToken token)
    {
        var settings = Settings.Instance.SettingsData;

        logger.Info("Connecting to {host}:{port} as {targetUser}:{listenKey}", settings.host, settings.port, settings.targetUser, settings.listenKey);
        var protocol = settings.host.Equals("127.0.0.1") || settings.host.Equals("localhost") ? "ws" : "wss";

        Socket = new();
        Socket.Options.SetRequestHeader("Authorization", "Basic " + Settings.Instance.GetAuthorization());
        await Socket.ConnectAsync(new Uri($"{protocol}://{settings.host}:{settings.port}/parameter-listen"), token);
    }

    public void Dispose()
    {
        CancelTokenSource.Cancel();
        Socket?.Dispose();
    }
}
