using System;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using NLog;
using ParamVR.ViewModels;

namespace ParamVR.Views;

public partial class LogView : Window
{
    private readonly FontFamily font = FontFamily.Parse("Consolas, Noto Sans Mono, JetBrains Mono, DejaVu Sans Mono, monospace");

    public LogView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is LogViewViewModel vm)
            {
                vm.LogLines.CollectionChanged += (_, evt) =>
                {
                    if (evt.Action == NotifyCollectionChangedAction.Add && evt.NewItems != null)
                    {
                        foreach (LogLineViewModel line in evt.NewItems)
                            AppendLine(line);

                        ScrollViewer.ScrollToEnd();
                    }
                };
            }
        };
    }
    
    private void AppendLine(LogLineViewModel line)
    {
        LogTextBlock.Inlines?.Add(new Run(line.Text + Environment.NewLine)
        {
            Foreground = line.Color ?? Foreground,
            FontFamily = font
        });
    }
}