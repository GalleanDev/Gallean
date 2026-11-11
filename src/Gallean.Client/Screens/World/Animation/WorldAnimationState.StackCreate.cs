namespace Gallean.Client;

internal closed partial record WorldAnimationState
{
    public sealed record StackCreate(WorldPosition Position) : WorldAnimationState;
}
