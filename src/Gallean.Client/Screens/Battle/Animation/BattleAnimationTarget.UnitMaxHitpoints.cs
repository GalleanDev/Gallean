namespace Gallean.Client;

internal closed partial record BattleAnimationTarget
{
    public sealed record UnitMaxHitpoints(BattleUnitId UnitId) : BattleAnimationTarget;
}
