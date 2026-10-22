namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class TurnStartingCityRiotRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldTurnStartingEvent ev, WorldState state)
        {
            WorldPlan plan = WorldPlan.For(state);

            List<WorldEvent> events = [];

            foreach (WorldFortification fortification in state.Fortifications.Values)
            {
                if (fortification is WorldCity city && city.OwnerId == ev.PlayerId && FortificationCalculator.IsRioting(city))
                {
                    if (city.RiotTurns > 1)
                    {
                        events.Add(new WorldCityRiotTurnsDecreasedEvent(city.Id, city.RiotTurns - 1));
                    }
                    else
                    {
                        events.Add(new WorldCityRiotEndedEvent(city.Id));

                        WorldStack? stack = plan.GetStackAt(city.Entrance);

                        if (stack is not null)
                        {
                            events.Add(new WorldStackMovementConsumedEvent(stack.Id, 0));
                            events.AddRange(DamageUnits(new WorldUnitGroup.Stack(stack.Id), stack.Units));
                        }

                        events.AddRange(DamageUnits(new WorldUnitGroup.Fortification(city.Id), city.Garrison));
                    }
                }
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }

        private List<WorldEvent> DamageUnits(WorldUnitGroup group, UnitGrid<WorldUnit> units)
        {
            List<WorldEvent> events = [];

            foreach ((WorldUnit unit, GridPosition _) in units.GetUnits())
            {
                int damage = unit.Hitpoints * Context.Globals.RiotDamagePercent / 100;
                int hitpoints = Math.Max(1, unit.Hitpoints - damage);

                if (hitpoints < unit.Hitpoints)
                {
                    events.Add(new WorldUnitDamagedEvent(group, unit.Id, hitpoints, SourcePlayerId: null));
                }
            }

            return events;
        }
    }
}
