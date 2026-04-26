namespace ParamVR.ViewModels;

public partial class InputDialogViewModel(string title, string label) : ViewModelBase
{
    public string Title { get; set; } = title;
    public string Label { get; set; } = label;
}
