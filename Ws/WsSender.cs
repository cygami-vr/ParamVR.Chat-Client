using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NLog;

namespace ParamVR.Ws;

internal class WsSender
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    public static WsSender Instance { get; private set; } = new();

    private static ClientWebSocket? Socket => WsController.Instance.Socket;

    private readonly object mutex = new();
    private readonly Dictionary<string, object> pendingUpdates = [];
    private bool scheduled = false;

    private WsSender() {}

    public static async Task Send(string s)
    {
        if (Socket == null || Socket.State != WebSocketState.Open)
            return;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            await Socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                WsController.Instance.CancelToken
            );

        }
        catch (Exception ex)
        {
            logger.Error(ex, "websocket error");
        }
    }
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

        await Send(JsonSerializer.Serialize(arr));
    }
}

internal class Parameter(string name, object value)
{
    public string name { get; set; } = name;
    public object value { get; set; } = value;
}
