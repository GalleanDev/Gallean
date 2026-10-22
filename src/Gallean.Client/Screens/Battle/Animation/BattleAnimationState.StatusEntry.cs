namespace Gallean.Client;

internal sealed partial record BattleAnimationState
{
    public sealed record StatusEntry(TimeSpan At, bool Frozen) : IBattleAnimationEntry;
}
