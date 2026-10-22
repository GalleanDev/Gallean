namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record StackBattle(WorldPosition Position) : WorldAnimationState;
}
