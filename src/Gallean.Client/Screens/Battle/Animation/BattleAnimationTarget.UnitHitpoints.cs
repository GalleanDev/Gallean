namespace Gallean.Client;

internal closed partial record BattleAnimationTarget
{
    public sealed record UnitHitpoints(BattleUnitId UnitId) : BattleAnimationTarget;
}
