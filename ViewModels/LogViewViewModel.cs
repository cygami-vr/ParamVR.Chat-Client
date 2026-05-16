using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using NLog;

namespace ParamVR.ViewModels;

/*
 * Avoid logging in this class or in related classes. It can cause a feedback loop.
 */
public partial class LogViewViewModel : ViewModelBase
{
    public ObservableCollection<LogLineViewModel> LogLines { get; } = [];

    private readonly string logPath;
    private FileSystemWatcher? _watcher;

    private const int MaxLines = 200;

    public LogViewViewModel()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        logPath = Path.Combine(appData, "ParamVR.Chat", "logs", "latest.log");

        foreach (var line in File.ReadLines(logPath).TakeLast(100))
            OnLogLineReceived(line);
        
        lastPosition = new FileInfo(logPath).Length;

        _watcher = new FileSystemWatcher(Path.GetDirectoryName(logPath)!, Path.GetFileName(logPath))
        {
            NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite
        };

        _watcher.Changed += (_, _) => ReadNewLines();
        _watcher.EnableRaisingEvents = true;
    }

    private void OnLogLineReceived(string line)
    {
        IBrush? color = null;

        if (line.Contains("WARN"))
            color = Brushes.Orange;
        else if (line.Contains("ERROR"))
            color = Brushes.Red;

        Dispatcher.UIThread.Post(() =>
        {
            LogLines.Add(new LogLineViewModel
            {
                Text = line,
                Color = color
            });

            while (LogLines.Count > MaxLines)
                LogLines.RemoveAt(0);
        });
    }

    private long lastPosition;

    private void ReadNewLines()
    {
        using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(lastPosition, SeekOrigin.Begin);

        using var reader = new StreamReader(stream);

        while (reader.ReadLine() is { } line)
            OnLogLineReceived(line);

        lastPosition = stream.Position;
    }

    public void Stop()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}