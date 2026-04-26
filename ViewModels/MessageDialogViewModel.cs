namespace ParamVR.ViewModels;

public partial class MessageDialogViewModel(string title, string message) : ViewModelBase
{
    public string Title { get; set; } = title;
    public string Message { get; set; } = message;
}
