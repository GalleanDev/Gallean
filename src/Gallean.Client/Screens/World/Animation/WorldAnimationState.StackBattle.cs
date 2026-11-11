namespace Gallean.Client;

internal closed partial record WorldAnimationState
{
    public sealed record StackBattle(WorldPosition Position) : WorldAnimationState;
}
