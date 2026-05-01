namespace ParamVR.ViewModels;

public partial class InputPromptViewModel(string title, string label) : ViewModelBase
{
    public string Title { get; set; } = title;
    public string Label { get; set; } = label;
}
