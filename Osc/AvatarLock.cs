using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace ParamVR.Osc;
internal class AvatarLock {
    public static AvatarLock Instance { get; private set; } = new();

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private string? currentAvatarId;
    private string? lockedAvatarId;
    private bool avatarLock;

    private CancellationTokenSource? cts = null;

    private AvatarLock() {}

    public void SetAvatar(string avatarId)
    {
        logger.Info("Avatar Id = {avatarId}", avatarId);
        currentAvatarId = avatarId;
        if (avatarLock)
        {
            if (currentAvatarId.Equals(lockedAvatarId))
            {
                cts?.Cancel();
            }
            else
            {
                logger.Info("Changed avatar while avatar locked.");
                StartAvatarLock();
            }
        }
    }

    public void SetAvatarLock(bool avatarLock)
    {
        logger.Info("Avatar Lock = {avatarLock}, Current = {currentAvatarId}", avatarLock, currentAvatarId);
        // Prevent starting an avatar lock if the current avatar is unknown.
        if (avatarLock)
        {
            if (currentAvatarId != null && currentAvatarId.Length > 0)
            {
                this.avatarLock = true;
                lockedAvatarId = currentAvatarId;
            }
            else
            {
                this.avatarLock = false;
                lockedAvatarId = null;
            }
        }
        else
        {
            this.avatarLock = false;
            lockedAvatarId = null;
        }
    }

    private void StartAvatarLock()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new();
        _ = StartAvatarLock(cts.Token);
    }

    private async Task StartAvatarLock(CancellationToken token)
    {
        try
        {
            while (avatarLock && currentAvatarId != null && lockedAvatarId != null && !currentAvatarId.Equals(lockedAvatarId))
            {
                logger.Info("Trying to switch to locked avatar.");
                OscSender.Instance.Send("/avatar/change", lockedAvatarId);
                await Task.Delay(75, token);
            }
        }
        catch (TaskCanceledException)
        {
            logger.Info("Avatar lock task canceled.");
        }
    }
}
