using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class TextPromptViewModel : AbstractScreenViewModel
{
    private readonly Func<string, Task> confirmAction;

    public TextPromptViewModel(
        TextSpans title,
        string message,
        string text,
        string placeholder,
        int maxLength,
        Func<string, Task> confirmAction)
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

    public TextSpans Title { get; }
    public string Message { get; }
    public string Placeholder { get; }
    public int MaxLength { get; }

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        CloseSelf();

        await confirmAction(Text);
    }
}
