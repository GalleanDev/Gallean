using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class ConfirmPromptViewModel(
    string title,
    TextSpans message,
    string confirmLabel,
    ResourceSetViewModel? confirmPrice,
    ConfirmPromptType type,
    Func<Task> confirmAction) : AbstractScreenViewModel
{
    public ConfirmPromptViewModel(
        string title,
        TextSpans message,
        string confirmLabel,
        ConfirmPromptType type,
        Func<Task> confirmAction)
        : this(title, message, confirmLabel, null, type, confirmAction)
    {
    }

    public string Title { get; } = title;
    public TextSpans Message { get; } = message;
    public string ConfirmLabel { get; } = confirmLabel;
    public ResourceSetViewModel? ConfirmPrice { get; } = confirmPrice;
    public ConfirmPromptType Type { get; } = type;

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        CloseSelf();

        await confirmAction();
    }
}
