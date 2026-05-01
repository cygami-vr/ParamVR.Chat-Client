namespace ParamVR.ViewModels;

public partial class MessageViewViewModel(string title, string message) : ViewModelBase
{
    public string Title { get; set; } = title;
    public string Message { get; set; } = message;
}
