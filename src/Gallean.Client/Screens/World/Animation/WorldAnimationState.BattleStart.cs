namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record BattleStart(WorldPosition Position) : WorldAnimationState;
}
