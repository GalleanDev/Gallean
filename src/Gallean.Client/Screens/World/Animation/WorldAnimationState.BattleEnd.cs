namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record BattleEnd(WorldPosition Position) : WorldAnimationState;
}
