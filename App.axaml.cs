using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ParamVR.ViewModels;
using ParamVR.Osc;
using ParamVR.Ws;

namespace ParamVR;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new SystemTrayViewModel();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            PvrChatOscQueryService.Instance.StartListening();
            _ = WsController.Instance.Restart();
            DataContext = new SystemTrayViewModel();
        }

        base.OnFrameworkInitializationCompleted();
    }
}