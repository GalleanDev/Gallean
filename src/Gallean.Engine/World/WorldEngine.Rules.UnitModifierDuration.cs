namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class TurnStartingUnitModifierDurationRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldTurnStartingEvent ev, WorldState state)
        {
            List<WorldEvent> events = [];

            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (stack.OwnerId == ev.PlayerId)
                {
                    AddEvents(events, new WorldUnitGroup.Stack(stack.Id), stack.Units);
                }
            }

            foreach (WorldFortification fortification in state.Fortifications.Values)
            {
                if (FortificationCalculator.GetOwner(fortification) == ev.PlayerId)
                {
                    AddEvents(events, new WorldUnitGroup.Fortification(fortification.Id), fortification.Garrison);
                }
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }

        private static void AddEvents(List<WorldEvent> events, WorldUnitGroup group, UnitGrid<WorldUnit> units)
        {
            foreach ((WorldUnit unit, GridPosition _) in units.GetUnits())
            {
                foreach (UnitModifier modifier in unit.Modifiers)
                {
                    if (modifier.RemainingTurns is not { } remainingTurns)
                    {
                        continue;
                    }

                    if (remainingTurns > 1)
                    {
                        events.Add(new WorldUnitModifierDurationDecreasedEvent(group, unit.Id, modifier.DefinitionId, remainingTurns - 1));
                    }
                    else
                    {
                        events.Add(new WorldUnitModifierExpiredEvent(group, unit.Id, modifier.DefinitionId));
                    }
                }
            }
        }
    }
}
