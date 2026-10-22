namespace Gallean.Engine;

internal abstract class BattleAbstractDrainAttackResolver : BattleAbstractAttackResolver
{
    protected override int GetHitDamage(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnitId targetId,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        return DamageCalculator.GetAttackDamage(state, context, attackerId, targetId, attack, attackStats);
    }

    protected override IReadOnlyList<BattleEvent> OnHit(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnitId targetId,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        return [];
    }
}
