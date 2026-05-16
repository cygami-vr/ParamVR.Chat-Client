using Avalonia.Controls;
using ParamVR.ViewModels;

namespace ParamVR.Views;

public partial class LogView : Window
{
    public LogView()
    {
        InitializeComponent();

        LogTextBox.PropertyChanged += (_, evt) =>
        {
            if (evt.Property == TextBox.TextProperty)
                ScrollViewer.ScrollToEnd();
        };

        Closing += (_, _) =>
        {
            if (DataContext is LogViewViewModel model)
                model.Stop();
        };
    }
}