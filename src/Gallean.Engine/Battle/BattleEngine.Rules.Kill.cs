namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class KillRule(IEngineContext context) : BattleEventRule(context)
    {
        public override EventRuleResult<BattleEvent> Visit(BattleUnitAttackHitEvent ev, BattleState state)
        {
            BattleUnit unit = state.GetUnit(ev.TargetId);

            if (ev.Damage > 0 && unit.Hitpoints <= 0)
            {
                return EventRuleResult<BattleEvent>.Append(new BattleUnitDiedEvent(ev.TargetId));
            }
            else
            {
                return EventRuleResult<BattleEvent>.Empty;
            }
        }
    }
}
