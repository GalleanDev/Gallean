namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class ExperienceRule(IEngineContext context) : BattleEventRule(context)
    {
        public override EventRuleResult<BattleEvent> Visit(BattleUnitDiedEvent ev, BattleState state)
        {
            BattleUnit deadUnit = state.GetUnit(ev.UnitId);

            if (deadUnit.SummonerId is not null)
            {
                return EventRuleResult<BattleEvent>.Empty;
            }

            List<BattleUnit> receivers = [];

            foreach (BattleUnitId unitId in state.GetAllUnitIds())
            {
                BattleUnit unit = state.GetUnit(unitId);

                if (unit.IsActive() && unit.SummonerId is null && unit.Side != deadUnit.Side)
                {
                    receivers.Add(unit);
                }
            }

            if (receivers.Count <= 0)
            {
                return EventRuleResult<BattleEvent>.Empty;
            }

            BattleState originalState = BattleTransformationCalculator.Restore(state, Context, deadUnit.Id);
            UnitStats deadUnitStats = Context.UnitStats.GetBattleUnitStats(originalState, deadUnit.Id);
            int experience = deadUnitStats.ExperienceKilled / receivers.Count;

            if (experience == 0)
            {
                return EventRuleResult<BattleEvent>.Empty;
            }

            List<BattleEvent> events = [];

            foreach (BattleUnit receiver in receivers)
            {
                UnitExperience receiverExperience = Context.UnitStats.GetBattleUnitExperience(state, receiver.Id);

                if (receiverExperience.CanGainExperience)
                {
                    events.Add(new BattleUnitExperienceGainedEvent(receiver.Id, receiver.ExperienceGained + experience));
                }
            }

            return EventRuleResult<BattleEvent>.Append([.. events]);
        }
    }
}
