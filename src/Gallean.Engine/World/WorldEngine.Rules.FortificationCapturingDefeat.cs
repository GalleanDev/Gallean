namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class FortificationCapturingDefeatRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldFortificationCapturingEvent ev, WorldState state)
        {
            if (state.Fortifications[ev.FortificationId] is not WorldCapital)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldPlayer previousOwner = state.Players[ev.PreviousOwnerId];

            if (!WorldHelpers.IsRacePlayable(previousOwner.Race))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldPlan plan = WorldPlan.For(state);

            if (!plan.IsPlayerDefeated(ev.PreviousOwnerId))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            return EventRuleResult<WorldEvent>.Append(new WorldPlayerDefeatedEvent(ev.PreviousOwnerId));
        }
    }
}
