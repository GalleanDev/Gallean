namespace Gallean.Engine;

internal abstract class BattleAbstractDamageOverTimeAttackResolver : BattleAbstractAttackResolver
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        if (target.IsRetreating())
        {
            return BattleCommandError.TargetIncorrect;
        }

        if (FindEffect(target) is { } effect && effect.Damage > attackStats.Damage)
        {
            return BattleCommandError.TargetIncorrect;
        }

        return null;
    }

    protected override IReadOnlyList<BattleEvent> OnHit(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnitId targetId,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        int remainingTurns;

        if (attack.IsInfinite)
        {
            remainingTurns = context.Random.Next(context.Globals.DamageOverTimeMinDurationTurns, context.Globals.DamageOverTimeMaxDurationTurns + 1);
        }
        else
        {
            remainingTurns = 1;
        }

        return [new BattleUnitEffectAppliedEvent(attackerId, targetId, CreateEffect(remainingTurns, attackStats.Damage))];
    }

    protected abstract BattleAbstractStatusEffect CreateEffect(int remainingTurns, int damage);

    protected abstract BattleAbstractDamageOverTimeEffect? FindEffect(BattleUnit target);
}
