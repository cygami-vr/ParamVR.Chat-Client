using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using ParamVR.Http;

namespace ParamVR.Ws;

internal class WsControllerNew
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static WsControllerNew Instance { get; private set; } = new();

    private readonly WsImpl ws = new();

    public event Action<Status>? StatusChanged;

    private WsControllerNew()
    {
        ws.StateChanged += StateChanged;
        ws.MessageReceived += MessageReceived;
    }

    private void StateChanged(WebSocketState state)
    {
        if (state == WebSocketState.Open)
            _ = Handshake();
    }

    public void UpdateStatus(Status status)
    {
        logger.Info("New status = {status}", status);
        StatusChanged?.Invoke(status);
    }

    private void MessageReceived(string msg)
    {
        var wsMsg = JsonSerializer.Deserialize<WsMessage>(msg);
        if (wsMsg != null)
            WsReceiver.HandleMessage(wsMsg);
    }

    public async Task Restart()
    {
        if (Settings.Instance.HasConnectionInfo())
        {
            UpdateStatus(Status.CONNECTING);
            await ws.Stop();
            Settings.Instance.SetConnectionInfo(ws);
            _ = ws.Start();
        }
        else
        {
            await ws.Stop();
            UpdateStatus(Status.DISCONNECTED);
        }
    }

    public async Task Dispose()
    {
        await ws.Stop();
        ws.Dispose();
    }

    private async Task Handshake()
    {
        var buffer = new byte[1024];
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var protocolVersion = await ws.Receive(buffer, cts.Token);
        logger.Info("Required protocol version = {protocolVersion}", protocolVersion);
        // validation is done by the server; can't trust the client
        await ws.Send(WsReceiver.Protocol_Version);
        if (!WsReceiver.Protocol_Version.Equals(protocolVersion))
        {
            await AppUtils.ShowMessage("Update required", "Your ParamVR.Chat Client is out of date. Please update and try again.");
            AppUtils.Exit();
            return;
        }
        string avatarId = await OscQueryHttpClient.Instance.GetAvatarId() ?? "";
        logger.Info("Sending avatar = {avatarId}", avatarId);
        await ws.Send(avatarId);
        WsReceiver.SendVRChatStatus();
        UpdateStatus(Status.CONNECTED);
    }

    public async Task Send(string msg) => await ws.Send(msg);
}
