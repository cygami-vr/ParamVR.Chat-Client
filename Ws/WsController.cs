using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using ParamVR.Http;

namespace ParamVR.Ws;

internal class WsController
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static WsController Instance { get; private set; } = new();

    private readonly WsImpl ws = new();

    public event Action<Status>? StatusChanged;

    private WsController()
    {
        ws.StateChanged += StateChanged;
        ws.MessageReceived += MessageReceived;
        ws.Handshake += Handshake;
    }

    private void StateChanged(WebSocketState state)
    {
        if (state == WebSocketState.None || state == WebSocketState.CloseSent || state == WebSocketState.CloseReceived || state == WebSocketState.Closed || state == WebSocketState.Aborted)
        {
            UpdateStatus(Status.FAILED_RETRYING);
        }
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
            await ws.Start();
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

    private async Task Handshake(CancellationToken ct)
    {
        logger.Info("Beginning handshake.");
        var buffer = new byte[1024];
        var protocolVersion = await ws.Receive(buffer, ct);
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
        logger.Info("Handshake complete.");
        UpdateStatus(Status.CONNECTED);
    }

    public async Task Send(string msg) => await ws.Send(msg);
}
