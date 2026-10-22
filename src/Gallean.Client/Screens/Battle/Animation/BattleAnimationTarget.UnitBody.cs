namespace Gallean.Client;

internal abstract partial record BattleAnimationTarget
{
    public sealed record UnitBody(BattleUnitId UnitId) : BattleAnimationTarget;
}
