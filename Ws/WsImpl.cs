using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace ParamVR.Ws;

public class WsImpl : IDisposable
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private ClientWebSocket? ws;

    public Uri? Uri { get; set; }
    public Dictionary<string, string> RequestHeaders { get; private set; } = [];

    private WebSocketState priorState = WebSocketState.None;

    public event Action<WebSocketState>? StateChanged;
    public event Action<string>? MessageReceived;

    private CancellationTokenSource? cts;

    private bool started;

    public bool IsDisposed { get; private set; }

    public WebSocketState? State => ws?.State;

    private void CheckForStateChange()
    {
        var current = ws?.State ?? WebSocketState.None;
        if (current != priorState)
        {
            logger.Info("Websocket state change {priorState} -> {current}", priorState, current);
            priorState = current;
            StateChanged?.Invoke(current);
        }
    }

    private void Cancel()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private async Task Close(string? msg) => await Close(WebSocketCloseStatus.NormalClosure, msg);

    private async Task Close(WebSocketCloseStatus stat, string? msg)
    {
        if (ws != null)
        {
            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
            {
                try
                {
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await ws.CloseAsync(stat, msg, timeoutCts.Token);
                }
                catch (Exception) {}
            }
            ws.Dispose();
            ws = null;
        }
        CheckForStateChange();
    }

    public async Task Start()
    {
        if (!started)
        {
            try
            {
                started = true;
                await Connect();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
    }

    public async Task Stop()
    {
        if (started)
        {
            try
            {
                started = false;
                await Close("Stopping");
            }
            catch (Exception ex)
            {
                logger.Warn(ex.ToString(), "Error closing websocket");
            }
        }
    }

    private async Task Connect()
    {
        try
        {
            Cancel();
            cts = new();
            var ct = cts.Token;
            await ConnectLoop(ct);
            await ReceiveLoop(ct);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "connect error");
        }
    }

    private async Task ConnectLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await TryConnect(ct);
                if (ws is { State: WebSocketState.Open })
                    return;
                await Task.Delay(5000, ct);
            }
        }
        catch (TaskCanceledException) {}
    }

    private async Task TryConnect(CancellationToken ct)
    {
        if (Uri == null)
            return;
        logger.Info("Attempting to connect websocket to {uri}", Uri);

        try
        {
            await Close("Reconnecting");
            ws = new();
            foreach(string key in RequestHeaders.Keys)
                ws.Options.SetRequestHeader(key, RequestHeaders[key]);
            await ws.ConnectAsync(Uri, ct);
            CheckForStateChange();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Websocket connect exception");
            await Close("Connection error");
        }
    }

    public async Task<string> Receive(byte[] segment, CancellationToken token)
    {
        var ws = this.ws;
        if (ws == null || ws.State != WebSocketState.Open)
            return "";
        
        var buffer = new List<byte>();

        while (true)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(segment), token);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                logger.Info("Received close message.");
                throw new TaskCanceledException();
            }

            buffer.AddRange(segment.Take(result.Count));

            if (result.EndOfMessage)
                return Encoding.UTF8.GetString([.. buffer]);
        }
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        try
        {
            var buffer = new byte[4096];
            while (ws != null && ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var msg = await Receive(buffer, ct);
                logger.Info("Received websocket message = {msg}", msg);
                MessageReceived?.Invoke(msg);
            }
        }
        catch (TaskCanceledException) {}
        catch (WebSocketException ex)
        {
            logger.Error(ex, "websocket error");
        }
        CheckForStateChange();
        if (!ct.IsCancellationRequested)
        {
            await Task.Delay(5000, ct);
            _ = Connect();
        }
    }

    public async Task Send(string msg)
    {
        // Get a reference to prevent race conditions.
        var ws = this.ws;

        if (ws == null || ws.State != WebSocketState.Open)
        {
            logger.Warn("Websocket not open, cannot send message.");
            return;
        }
        logger.Info("Sending websocket message {msg}", msg);

        var bytes = Encoding.UTF8.GetBytes(msg);
        using var timeoutCts = new CancellationTokenSource(1000);

        try
        {
            await ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                timeoutCts.Token
            );
        }
        catch (WebSocketException ex)
        {
            logger.Error(ex, "Error sending message. Canceling receive loop.");
            Cancel();
        }
        catch (TaskCanceledException) {}
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
                ws?.Dispose();
                cts?.Dispose();
            }

            // set large fields to null
            ws = null;
            IsDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
