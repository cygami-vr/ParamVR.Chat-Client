using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using NLog;
using ParamVR.ViewModels;
using ParamVR.Views;

namespace ParamVR;

class AppUtils
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static Task ShowMessageDialog(string title, string label)
    {
        var dialog = new InputDialog
        {
            DataContext = new InputDialogViewModel(title, label)
        };

        var tcs = new TaskCompletionSource<bool>();
        dialog.Closed += (_, _) => tcs.TrySetResult(true);
        dialog.Show();
        return tcs.Task;
    }

    public static Task<string?> ShowInputDialog(string title, string label)
    {
        var dialog = new InputDialog
        {
            DataContext = new InputDialogViewModel(title, label)
        };

        var tcs = new TaskCompletionSource<string?>();
        dialog.Closed += (_, _) => tcs.TrySetResult(dialog.Result);
        dialog.Show();
        return tcs.Task;
    }

    public static void Exit()
    {
        logger.Trace("Exiting.");
        LogManager.Shutdown();

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
        else
        {
            Environment.Exit(0);
        }
    }
}