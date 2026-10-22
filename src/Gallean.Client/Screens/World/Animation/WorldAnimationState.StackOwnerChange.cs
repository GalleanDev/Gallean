namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record StackOwnerChange(WorldPosition Position) : WorldAnimationState;
}
