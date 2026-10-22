namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record Pause(TimeSpan Duration) : WorldAnimationState;
}
