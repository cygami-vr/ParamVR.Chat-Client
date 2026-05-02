using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NLog;
using ParamVR.Http;
using ParamVR.Osc;
using ParamVR.Views;
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

    private static void Open(string fileName) {
        Process.Start(new ProcessStartInfo {
            FileName = fileName,
            UseShellExecute = true
        });
    }

    public SystemTrayViewModel()
    {
        WsController.Instance.StatusChanged += newStatus =>
        {
            switch (newStatus)
            {
                case Status.CONNECTING:
                    StatusText = "Connecting to ParamVR.Chat, please wait...";
                    break;
                case Status.CONNECTED:
                    StatusText = "Connected.";
                    break;
                case Status.DISCONNECTED:
                    StatusText = "Not connected.";
                    break;
                case Status.FAILED_RETRYING:
                    StatusText = "Connection failed, retrying...";
                    break;
            }
        };
    }

    public async Task Connect()
    {
        string? targetUser = await AppUtils.ShowInputPrompt("Username", "Please enter your ParamVR.Chat username:");

        if (targetUser == null || targetUser.Length == 0)
            return;
        
        string? listenKey = await AppUtils.ShowInputPrompt("Listen key", "Please enter the listen key obtained from the ParamVR.Chat website:");

        if (listenKey == null || listenKey.Length == 0)
            return;

        Settings.Instance.SetConnectionInfo(targetUser, listenKey);
    }

    public void EmergencyUnlock()
    {
        MuteLock.Instance.SetMuteLock(false);
        AvatarLock.Instance.SetAvatarLock(false);
        _ = PvrHttpClient.Instance.Post("client/parameter/emergency-unlock", null, null);
    }

    public void BrowsePvrAppData()
    {
        var pvrAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ParamVR.Chat");
        Open(pvrAppData);
    }

    public void OpenLogViewer()
    {
        var view = new LogView()
        {
            DataContext = new LogViewViewModel()
        };
        view.Show();
    }

    public void OpenIconCredit() => Open("https://www.flaticon.com/free-icons/toggle-button");

    public void OpenLicenseInformation() => Open(Path.Combine(AppContext.BaseDirectory, "Assets", "licenses.txt"));

    public void Exit() => AppUtils.Exit();
}
