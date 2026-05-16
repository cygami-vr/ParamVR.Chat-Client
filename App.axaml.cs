using Avalonia;
using Avalonia.Threading;
using Avalonia.Markup.Xaml;
using ParamVR.ViewModels;
using ParamVR.Osc;
using ParamVR.Ws;
using NLog;

namespace ParamVR;

public partial class App : Application
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new SystemTrayViewModel();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            logger.Error(e.Exception, "Unhandled exception");
            e.Handled = true;
        };

        PvrChatOscQueryService.Instance.StartListening();
        _ = WsController.Instance.Restart();

        base.OnFrameworkInitializationCompleted();
    }
}