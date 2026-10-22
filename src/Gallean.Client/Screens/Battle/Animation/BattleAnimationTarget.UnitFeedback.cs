namespace Gallean.Client;

internal abstract partial record BattleAnimationTarget
{
    public sealed record UnitFeedback(BattleUnitId UnitId) : BattleAnimationTarget;
}
