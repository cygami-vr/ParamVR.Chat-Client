using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ParamVR.Views;

public partial class MessageDialog : Window
{
    public MessageDialog()
    {
        InitializeComponent();
    }

    private void OnOk(object? sender, RoutedEventArgs evt) => Close(null);
}