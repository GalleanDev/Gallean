using Gallean.Common;

namespace Gallean.Engine;

internal sealed class CureAttackResolver : BattleAbstractAttackResolver, IStaticInstance<CureAttackResolver>
{
    protected override BattleCommandError? ValidateTarget(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleUnit target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        if (!target.StatusEffects.Any(effect => effect.IsCurable()))
        {
            return BattleCommandError.TargetNotCurable;
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
        BattleUnit target = state.GetUnit(targetId);
        BattleTransformStatusEffect? transformEffect = BattleTransformationCalculator.GetTransformEffect(target);
        List<BattleEvent> events = [];

        if (transformEffect is not null && transformEffect.IsCurable())
        {
            events.Add(BattleTransformationCalculator.GetRestoreEvent(state, context, targetId, transformEffect));
        }

        events.Add(new BattleUnitCuredEvent(targetId));

        return events;
    }
}
