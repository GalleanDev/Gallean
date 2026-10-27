namespace Gallean.Client;

internal abstract partial record BattleAnimationTarget
{
    public sealed record UnitMaxHitpoints(BattleUnitId UnitId) : BattleAnimationTarget;
}
