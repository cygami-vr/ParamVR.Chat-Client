using System;
using System.IO;
using System.Linq;

namespace ParamVR.ViewModels;

/*
 * Avoid logging in this class or in related classes. It can cause a feedback loop.
 */
public partial class LogViewViewModel : ViewModelBase
{
    public string LogText { get; set; } = "";

    private readonly string logPath;
    private FileSystemWatcher? _watcher;

    public LogViewViewModel()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        logPath = Path.Combine(appData, "ParamVR.Chat", "logs", "latest.log");

        foreach (var line in File.ReadLines(logPath).TakeLast(100))
            OnLogLineReceived(line);

        _watcher = new FileSystemWatcher(Path.GetDirectoryName(logPath)!, Path.GetFileName(logPath))
        {
            NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite
        };
        _watcher.Changed += (_, _) => ReadNewLines();
        _watcher.EnableRaisingEvents = true;
    }

    private void OnLogLineReceived(string line)
    {
        LogText += line + Environment.NewLine;
        if (LogText.Length > 10000)
            LogText = LogText[^8000..];
        OnPropertyChanged();
        OnPropertyChanged(nameof(LogText));
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
