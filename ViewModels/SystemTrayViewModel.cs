using System.Threading.Tasks;
using NLog;
using ParamVR.Http;
using ParamVR.Osc;
using ParamVR.Ws;

namespace ParamVR.ViewModels;

public partial class SystemTrayViewModel : ViewModelBase
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    
    private string _statusText = "Not connected";

    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusLabelText));
        }
    }

    public string StatusLabelText => $"Status: {StatusText}";

    public SystemTrayViewModel()
    {
        WsController.Instance.StatusChanged += newStatus =>
        {
            switch (newStatus) {
                case Status.CONNECTING:
                    StatusText = "Status: Connecting to ParamVR.Chat, please wait...";
                    break;
                case Status.CONNECTED:
                    StatusText = "Status: Connected.";
                    break;
                case Status.DISCONNECTED:
                    StatusText = "Status: Not connected.";
                    break;
                case Status.FAILED_RETRYING:
                    StatusText = "Status: Connection failed, retrying...";
                    break;
            }
        };
    }

    public async Task Connect()
    {
        string? targetUser = await AppUtils.ShowInputDialog("Username", "Please enter your ParamVR.Chat username:");

        if (targetUser == null || targetUser.Length == 0)
            return;
        
        string? listenKey = await AppUtils.ShowInputDialog("Listen key", "Please enter the listen key obtained from the ParamVR.Chat website:");

        if (listenKey == null || listenKey.Length == 0)
            return;

        Settings.Instance.SetConnectionInfo(targetUser, listenKey);
    }

    public void EmergencyUnlock()
    {
        MuteLock.Instance.SetMuteLock(false);
        _ = PvrHttpClient.Instance.Post("client/parameter/emergency-unlock", null, null);
    }

    public void Exit() => AppUtils.Exit();
}
