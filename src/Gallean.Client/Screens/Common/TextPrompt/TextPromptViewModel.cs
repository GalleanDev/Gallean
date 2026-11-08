using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class TextPromptViewModel : AbstractScreenViewModel
{
    private readonly Action<string> confirmAction;

    public TextPromptViewModel(
        string title,
        string message,
        string text,
        string placeholder,
        int maxLength,
        Action<string> confirmAction)
    {
        this.confirmAction = confirmAction;

        Title = title;
        Message = message;
        Text = text;
        Placeholder = placeholder;
        MaxLength = maxLength;
    }

    [ObservableProperty]
    public partial string Text { get; set; }

    public string Title { get; }
    public string Message { get; }
    public string Placeholder { get; }
    public int MaxLength { get; }

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    [RelayCommand]
    private void Confirm()
    {
        CloseSelf();

        confirmAction(Text);
    }
}
