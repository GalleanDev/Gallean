namespace Gallean.Client;

internal closed partial record WorldAnimationState
{
    public sealed record BattleEnd(WorldPosition Position) : WorldAnimationState;
}
