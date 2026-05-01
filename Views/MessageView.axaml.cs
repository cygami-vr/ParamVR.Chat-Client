using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ParamVR.Views;

public partial class MessageView : Window
{
    public MessageView()
    {
        InitializeComponent();
    }

    private void OnOk(object? sender, RoutedEventArgs evt) => Close(null);
}