using System.Diagnostics;
using NLog;
using System.Linq;
using System;
using ParamVR.Osc;

namespace ParamVR.Ws;

internal class WsReceiver
{

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static WsReceiver Instance { get; private set; } = new();
    public static readonly string Protocol_Version = "0.3";

    private WsReceiver() {}

    public static void SendVRChatStatus()
    {
        var isOpen = Process.GetProcesses()
            .Any(p => p.ProcessName.Contains("vrchat", StringComparison.OrdinalIgnoreCase));

        logger.Info("VRC Open = {isOpen}", isOpen);
        WsSender.Instance.Enqueue("/chat/paramvr/vrcOpen", isOpen);
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
