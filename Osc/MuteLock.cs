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
            {
                cts?.Cancel();
                // Resetting to 0 helps prevent the toggle mute shortcut from breaking.
                SendMute(0);
            }
            else
            {
                logger.Info("Unmuted while mute locked.");
                StartMuteLock();
            }
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
        cts?.Cancel();
        cts?.Dispose();
        cts = new();
        _ = StartMuteLock(cts.Token);
    } 

    private async Task StartMuteLock(CancellationToken token)
    {
        try
        {
            while (muteLock && !muted)
            {
                logger.Info("Trying to mute");
                await Task.Delay(75, token);
                SendMute(1);
                await Task.Delay(75, token);
                SendMute(0);
            }
        }
        catch (TaskCanceledException) {}
    }
}
