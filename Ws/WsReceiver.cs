using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using NLog;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using ParamVR.Http;
using ParamVR.Osc;

namespace ParamVR.Ws;

internal class WsReceiver
{

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static WsReceiver Instance { get; private set; } = new();
    public static readonly string Protocol_Version = "0.3";

    private WsReceiver() {}

    private static async Task Handshake(ClientWebSocket ws)
    {
        var protocolVersion = await Receive(ws);
        logger.Info("Required protocol version = {protocolVersion}", protocolVersion);
        // validation is done by the server; can't trust the client
        await WsSender.Send(Protocol_Version);
        if (!Protocol_Version.Equals(protocolVersion))
        {
            await AppUtils.ShowMessage("Update required", "Your ParamVR.Chat Client is out of date. Please update and try again.");
            AppUtils.Exit();
            return;
        }
        string avatarId = await OscQueryHttpClient.Instance.GetAvatarId() ?? "";
        logger.Info("Sending avatar = {avatarId}", avatarId);
        await WsSender.Send(avatarId);
        SendVRChatStatus();
    }

    public static void SendVRChatStatus()
    {
        var isOpen = Process.GetProcesses()
            .Any(p => p.ProcessName.Contains("vrchat", StringComparison.OrdinalIgnoreCase));

        logger.Info("VRC Open = {isOpen}", isOpen);
        WsSender.Instance.Enqueue("/chat/paramvr/vrcOpen", isOpen);
    }

    private static async Task<string> Receive(ClientWebSocket ws)
    {
        return await Receive(ws, new byte[1024]);
    }

    private static async Task<string> Receive(ClientWebSocket ws, byte[] segment)
    {
        if (ws == null)
            return "";
        var buffer = new List<byte>();

        while (true)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(segment), WsController.Instance.CancelToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                logger.Trace("Received close message.");
                throw new TaskCanceledException();
            }

            buffer.AddRange(segment.Take(result.Count));

            if (result.EndOfMessage)
                return Encoding.UTF8.GetString([.. buffer]);
        }
    }

    public static async Task StartReceiveLoop(ClientWebSocket ws)
    {
        try
        {
            await Handshake(ws);
            logger.Info("Starting receive loop.");
            WsController.Instance.UpdateStatus(Status.CONNECTED);
            var segment = new byte[1024];

            while (ws.State == WebSocketState.Open)
            {
                var json = await Receive(ws, segment);
                var msg = JsonSerializer.Deserialize<WsMessage>(json);
                if (msg != null)
                    HandleMessage(msg);
            }

            logger.Info("Receive loop exited normally.");

        }
        catch (WebSocketException ex)
        {
            logger.Error(ex, "websocket error");
            WsController.Instance.UpdateStatus(Status.FAILED_RETRYING);
        }
        catch (TaskCanceledException) {}
    }

    public static void HandleMessage(WsMessage msg)
    {
        if (msg.parameter != null)
        {
            logger.Info("Received param over websocket: {name} = {value}", msg.parameter.name, msg.parameter.value);
            if (msg.parameter.name == "chat-paramvr-activity")
                SendVRChatStatus();
            else if (msg.parameter.name == "chat-paramvr-mutelock")
                MuteLock.Instance.SetMuteLock("true".Equals(msg.parameter.value));
            else if (msg.parameter.name == "chat-paramvr-avatarlock")
                AvatarLock.Instance.SetAvatarLock("true".Equals(msg.parameter.value));
            else
                SendOsc(msg.parameter);
        }
        else if (msg.vrcUuid != null)
        {
            logger.Info("Received avatar change over websocket to: {vrcUuid}", msg.vrcUuid);
            OscSender.Instance.Send("/avatar/change", msg.vrcUuid);
        }
        else
            logger.Warn("Empty websocket message received");
    }

    // try this?
    // public enum ParamType { Int = 1, Float = 2, Bool = 3 }

    private static void SendOsc(WsParameter param)
    {
        var addr = "/avatar/parameters/" + param.name;

        switch (param.dataType)
        {
            case 1:
                OscSender.Instance.Send(addr, int.Parse(param.value));
                break;
            case 2:
                OscSender.Instance.Send(addr, (float)double.Parse(param.value));
                break;
            case 3:
                OscSender.Instance.Send(addr, bool.Parse(param.value));
                break;
        }
    }
}

internal class WsMessage
{
    public string type { get; set; }
    public WsParameter? parameter { get; set; }
    public string? vrcUuid { get; set; }
}

internal class WsParameter
{
    public string name { get; set; }
    public string value { get; set; }
    public short dataType { get; set; }
}
