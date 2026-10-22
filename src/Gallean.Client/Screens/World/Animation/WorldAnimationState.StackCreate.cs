namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record StackCreate(WorldPosition Position) : WorldAnimationState;
}
