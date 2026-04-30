using System;
using System.Linq;
using BlobHandles;
using BuildSoft.OscCore;
using NLog;
using ParamVR.Ws;
using VRC.OSCQuery;

namespace ParamVR.Osc;

internal class OscListener: IDisposable
{

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static OscListener Instance { get; private set; } = new();

    private static readonly string[] activityParams =
    [
        "Angular", "Velocity", "GestureRight", "GestureLeft"
    ];

    private static readonly string[] ignoredParams =
    [
        "InStation", "Seated", "Grounded", "Voice", "Viseme", "TrackingType", "Upright"
    ];
    public int PortIn { get; private set; }
    private OscServer? receiver;
    private long lastMovementUpdate = -1;

    private static readonly DateTime unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private OscListener() {}

    public int StartListening()
    {
        receiver?.Dispose();
        PortIn = Extensions.GetAvailableUdpPort();
        logger.Info("Creating OscServer. In = {portIn}", PortIn);
        receiver = new OscServer(PortIn);
        receiver.AddMonitorCallback(Instance.OnMessage);
        receiver.Start();
        return PortIn;
    }

    // Until I find a better alternative.
    private static long CurrentTimeMillis()
        => (long)(DateTime.UtcNow - unixEpoch).TotalMilliseconds;

    public void OnMessage(BlobString addr, OscMessageValues msg)
    {
        var sAddr = addr.ToString();
        var isActivity = IsActivity(sAddr);

        if (isActivity)
        {
            var time = CurrentTimeMillis();
            if (time - lastMovementUpdate > 60000)
            {
                WsSender.Instance.Enqueue("/chat/paramvr/lastActivity", time);
                lastMovementUpdate = time;
            }
        }
        else if (!ShouldIgnore(sAddr) && (sAddr.StartsWith("/avatar/parameters") || sAddr.Equals("/avatar/change")))
        {

            var tag = msg.GetTypeTag(0);

            object? value = tag switch
            {
                TypeTag.String => msg.ReadStringElement(0),
                TypeTag.Int32 => msg.ReadIntElement(0),
                TypeTag.Float32 => msg.ReadFloatElement(0),
                TypeTag.True => true,
                TypeTag.False => false,
                _ => null
            };

            if (value != null)
            {
                WsSender.Instance.Enqueue(sAddr, value);

                if (sAddr.Equals("/avatar/parameters/MuteSelf"))
                    MuteLock.Instance.SetMuted((bool)value);
                else if (sAddr.Equals("/avatar/change"))
                    AvatarLock.Instance.SetAvatar((string)value);
            }
        }
    }

    private static bool IsActivity(string addr)
        => activityParams.Any(p => addr.StartsWith("/avatar/parameters/" + p));
    
    private static bool ShouldIgnore(string addr)
        => ignoredParams.Any(p => addr.StartsWith("/avatar/parameters/" + p));

    public void Dispose() => receiver?.Dispose();
}
