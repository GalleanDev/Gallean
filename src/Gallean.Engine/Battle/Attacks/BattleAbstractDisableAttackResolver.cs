namespace Gallean.Engine;

internal abstract class BattleAbstractDisableAttackResolver : BattleAbstractAttackResolver
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack)
    {
        if (target.IsDisabled())
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
            remainingTurns = context.Random.Next(context.Globals.DisableEffectMinDurationTurns, context.Globals.DisableEffectMaxDurationTurns + 1);
        }
        else
        {
            remainingTurns = context.Globals.DisableEffectMinDurationTurns;
        }

        return [new BattleUnitEffectAppliedEvent(attackerId, targetId, CreateEffect(remainingTurns))];
    }

    protected abstract BattleAbstractStatusEffect CreateEffect(int remainingTurns);
}
