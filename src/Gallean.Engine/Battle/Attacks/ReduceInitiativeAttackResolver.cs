using Gallean.Common;

namespace Gallean.Engine;

internal sealed class ReduceInitiativeAttackResolver : BattleAbstractAttackResolver, IStaticInstance<ReduceInitiativeAttackResolver>
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        if (target.IsRetreating() || target.IsDisabled())
        {
            return BattleCommandError.TargetIncorrect;
        }

        if (target.HasStatusEffect<BattleReduceInitiativeStatusEffect>())
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
        BattleReduceInitiativeStatusEffect effect = new(context.Globals.ReduceInitiativePercent, attack.IsInfinite);

        return [new BattleUnitEffectAppliedEvent(attackerId, targetId, effect)];
    }
}
