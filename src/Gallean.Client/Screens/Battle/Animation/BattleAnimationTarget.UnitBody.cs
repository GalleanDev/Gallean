namespace Gallean.Client;

internal closed partial record BattleAnimationTarget
{
    public sealed record UnitBody(BattleUnitId UnitId) : BattleAnimationTarget;
}
