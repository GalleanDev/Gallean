namespace Gallean.Client;

internal closed partial record WorldAnimationState
{
    public sealed record BattleStart(WorldPosition Position) : WorldAnimationState;
}
