namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class FortificationCapturingRiotRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldFortificationCapturingEvent ev, WorldState state)
        {
            if (state.Fortifications[ev.FortificationId] is not WorldCity city)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            if (!FortificationCalculator.IsRioting(city))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            return EventRuleResult<WorldEvent>.Append(new WorldCityRiotEndedEvent(city.Id));
        }
    }
}
