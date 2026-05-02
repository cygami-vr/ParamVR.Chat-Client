using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using NLog;
using ParamVR.Http;
using ParamVR.Osc;
using ParamVR.ViewModels;
using ParamVR.Views;
using ParamVR.Ws;

namespace ParamVR;

internal class AppUtils
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static Task ShowMessage(string title, string msg)
    {
        var dialog = new MessageView
        {
            DataContext = new MessageViewViewModel(title, msg)
        };

        var tcs = new TaskCompletionSource<bool>();
        dialog.Closed += (_, _) => tcs.TrySetResult(true);
        dialog.Show();
        return tcs.Task;
    }

    public static Task<string?> ShowInputPrompt(string title, string label)
    {
        var dialog = new InputPrompt
        {
            DataContext = new InputPromptViewModel(title, label)
        };

        var tcs = new TaskCompletionSource<string?>();
        dialog.Closed += (_, _) => tcs.TrySetResult(dialog.Result);
        dialog.Show();
        return tcs.Task;
    }

    public static void Exit()
    {
        logger.Trace("Exiting.");
        OscQueryHttpClient.Instance.Dispose();
        PvrHttpClient.Instance.Dispose();   
        OscListener.Instance.Dispose();
        OscSender.Instance.Dispose();
        PvrChatOscQueryService.Instance.Dispose();
        _ = WsController.Instance.Dispose();
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