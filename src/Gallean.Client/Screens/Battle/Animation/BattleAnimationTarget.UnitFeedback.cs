namespace Gallean.Client;

internal closed partial record BattleAnimationTarget
{
    public sealed record UnitFeedback(BattleUnitId UnitId) : BattleAnimationTarget;
}
