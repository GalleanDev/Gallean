namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class AttackRule(IEngineContext context) : BattleEventRule(context)
    {
        public override EventRuleResult<BattleEvent> Visit(BattleUnitAttackStepEvent ev, BattleState state)
        {
            if (state.Outcome is not null)
            {
                return EventRuleResult<BattleEvent>.Consume();
            }

            if (state.TryGetUnit(ev.AttackerId) is null || state.TryGetUnit(ev.TargetId) is null)
            {
                return EventRuleResult<BattleEvent>.Consume();
            }

            BattleAttackResult attackResult = BattleAttackCalculator.ResolveAttackStep(
                state,
                Context,
                ev.AttackerId,
                ev.TargetId,
                ev.AttackIds,
                ev.AccuracyReduction);

            if (attackResult.Failed)
            {
                return EventRuleResult<BattleEvent>.Consume();
            }

            return EventRuleResult<BattleEvent>.Consume([.. attackResult.Events]);
        }
    }
}
