using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NLog;

namespace ParamVR.Ws;

internal class WsSender
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static WsSender Instance { get; private set; } = new();

    private readonly object mutex = new();
    private readonly Dictionary<string, object> pendingUpdates = [];
    private bool scheduled = false;

    private WsSender() {}

    public void Enqueue(string name, object value)
    {
        lock (mutex)
        {
            pendingUpdates[name] = value;

            if (!scheduled)
            {
                scheduled = true;
                _ = ScheduleSendAsync();
            }
        }
    }
    private async Task ScheduleSendAsync()
    {

        await Task.Delay(1000);
        Dictionary<string, object> updatesToSend;

        lock (mutex)
        {
            updatesToSend = new Dictionary<string, object>(pendingUpdates);
            pendingUpdates.Clear();
            scheduled = false;
        }

        if (updatesToSend.Count == 0)
            return;

        var arr = updatesToSend.Select(pair =>
        {
            return new Parameter(pair.Key, pair.Value);
        }).ToArray();

        await WsController.Instance.Send(JsonSerializer.Serialize(arr));
    }
}

internal class Parameter(string name, object value)
{
    public string name { get; set; } = name;
    public object value { get; set; } = value;
}
