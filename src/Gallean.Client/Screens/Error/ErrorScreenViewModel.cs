using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class ErrorScreenViewModel : AbstractScreenViewModel
{
    private readonly Func<Task> action;

    public ErrorScreenViewModel(string message, string actionLabel)
    {
        Message = message;
        ActionLabel = actionLabel;
        action = async () =>
        {
            CloseSelf();
        };
    }

    public ErrorScreenViewModel(string message, string actionLabel, Action action, string? details = null)
        : this(message, actionLabel, async () =>
        {
            action();
        }, details)
    {
    }

    public ErrorScreenViewModel(string message, string actionLabel, Func<Task> action, string? details = null)
    {
        Message = message;
        ActionLabel = actionLabel;
        Details = details;
        this.action = action;
    }

    public string Message { get; }
    public string ActionLabel { get; }
    public string? Details { get; }

    public override ScreenMusic Music => ScreenMusic.None;

    [RelayCommand]
    private async Task ExecuteActionAsync()
    {
        await action();
    }
}
