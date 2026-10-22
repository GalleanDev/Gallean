namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class AllianceStartedAddSpiesRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldAllianceStartedEvent ev, WorldState state)
        {
            List<WorldEvent> events = [];

            if (!SpyCalculator.SpiesOnPlayer(state, ev.PlayerId, ev.TargetPlayerId))
            {
                events.Add(new WorldSpyAddedEvent(ev.PlayerId, ev.TargetPlayerId));
            }

            if (!SpyCalculator.SpiesOnPlayer(state, ev.TargetPlayerId, ev.PlayerId))
            {
                events.Add(new WorldSpyAddedEvent(ev.TargetPlayerId, ev.PlayerId));
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }
    }
}
