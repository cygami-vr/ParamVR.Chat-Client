using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ParamVR.Views;

public partial class InputPrompt : Window
{
    public string? Result { get; private set; }

    public InputPrompt()
    {
        InitializeComponent();
    }

    private void OnOk(object? sender, RoutedEventArgs evt)
    {
        var input = this.FindControl<TextBox>("InputBox");
        Result = input?.Text;
        Close(Result);
    }

    private void OnCancel(object? sender, RoutedEventArgs evt) => Close(null);
}