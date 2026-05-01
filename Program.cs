using Avalonia;
using Avalonia.Controls;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ParamVR;

sealed class Program
{

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private static Mutex? mutex;
    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            mutex = new Mutex(false, @"Global\ParamVR.Chat-Client_Lock");
            try
            {
                if (!mutex.WaitOne(0, false))
                {
                    Console.WriteLine("Another instance of the ParamVR.Chat Client is already running; exiting.");
                    mutex = null; // prevent unnecessary release()
                    return;
                }
            }
            catch (AbandonedMutexException) {}

            logger.Info("OnStartup PID={pid}", Environment.ProcessId);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    logger.Error(ex, "Unhandled exception");
                else
                    logger.Error("Unhandled " + e.ExceptionObject.GetType().FullName);
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                logger.Error(e.Exception, "Unhandled exception");
                e.SetObserved();
            };

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
        }
        finally
        {
            mutex?.ReleaseMutex();
            mutex?.Dispose();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
