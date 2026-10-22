namespace Gallean.Client;

internal sealed class LoadingScreenViewModel : AbstractLiveBackgroundScreenViewModel
{
    private static readonly TimeSpan DefaultRevealDelay = TimeSpan.Zero;
    private static readonly TimeSpan DefaultRevealDuration = TimeSpan.FromMilliseconds(125);

    public LoadingScreenViewModel(string message)
        : this(message, DefaultRevealDelay, DefaultRevealDuration)
    {
    }

    public LoadingScreenViewModel(string message, TimeSpan revealDelay, TimeSpan revealDuration)
    {
        Message = message;
        RevealDelay = revealDelay;
        RevealDuration = revealDuration;
    }

    public string Message { get; }
    public override TimeSpan RevealDelay { get; }
    public override TimeSpan RevealDuration { get; }

    protected override void OnOpened()
    {
        ShowWaitCursorAsync().FileAndForget();
    }

    private async Task ShowWaitCursorAsync()
    {
        await Task.Delay(RevealDelay + RevealDuration);

        CursorType = AssetAnimationKey.CursorType.Wait;
    }
}
