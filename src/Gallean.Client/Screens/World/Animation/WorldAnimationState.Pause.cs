namespace Gallean.Client;

internal closed partial record WorldAnimationState
{
    public sealed record Pause(TimeSpan Duration) : WorldAnimationState;
}
