namespace Gallean.Client;

internal abstract partial record BattleAnimationTarget
{
    public sealed record UnitHitpoints(BattleUnitId UnitId) : BattleAnimationTarget;
}
