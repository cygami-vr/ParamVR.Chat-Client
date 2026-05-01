using Avalonia;
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
        PvrChatOscQueryService.Instance.StartListening();
        // _ = WsController.Instance.Restart();
        _ = WsControllerNew.Instance.Restart();

        base.OnFrameworkInitializationCompleted();
    }
}