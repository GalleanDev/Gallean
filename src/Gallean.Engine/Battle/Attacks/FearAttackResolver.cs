using Gallean.Common;

namespace Gallean.Engine;

internal sealed class FearAttackResolver : BattleAbstractAttackResolver, IStaticInstance<FearAttackResolver>
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
        BattleSideModifiers targetModifiers = state.Modifiers.GetModifiers(target.Side);

        if (targetModifiers.CanRetreat)
        {
            return [new BattleUnitRetreatingEvent(targetId)];
        }
        else
        {
            BattleParalyzeStatusEffect paralysis = new(context.Globals.DisableEffectMinDurationTurns);

            return [new BattleUnitEffectAppliedEvent(attackerId, targetId, paralysis)];
        }
    }
}
