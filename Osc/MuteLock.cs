using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace ParamVR.Osc;
internal class MuteLock
{
    public static MuteLock Instance { get; private set; } = new();

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private bool muted;
    private bool muteLock;

    private CancellationTokenSource? cts = null;

    private MuteLock() {}

    public void SetMuted(bool muted)
    {
        logger.Info("Muted = {muted}", muted);
        this.muted = muted;
        if (muteLock)
        {
            if (muted)
                cts?.Cancel();
            else
                StartMuteLock();
        }
    }

    public static void SendMute(int mute)
    {
        OscSender.Instance.Send("/input/Voice", mute);
    }

    public void SetMuteLock(bool muteLock)
    {
        this.muteLock = muteLock;
        if (muteLock && !muted)
            StartMuteLock();
    }

    private void StartMuteLock()
    {
        logger.Info("Unmuted while mute locked.");
        cts?.Cancel();
        cts?.Dispose();
        cts = new();
        _ = StartMuteLock(cts.Token);
    } 

    private async Task StartMuteLock(CancellationToken token)
    {
        try
        {
            while (muteLock && !muted && !token.IsCancellationRequested)
            {
                logger.Info("Trying to mute");
                await Task.Delay(75);
                SendMute(1);
                await Task.Delay(75, token);
                SendMute(0);
            }
        }
        catch (TaskCanceledException) {}
    }
}
