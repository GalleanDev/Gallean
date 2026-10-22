namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class AllianceEndedRemoveSpiesRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldAllianceEndedEvent ev, WorldState state)
        {
            List<WorldEvent> events = [];

            if (state.Players[ev.PlayerId].SpiedPlayers.Contains(ev.TargetPlayerId))
            {
                events.Add(new WorldSpyRemovedEvent(ev.PlayerId, ev.TargetPlayerId));
            }

            if (state.Players[ev.TargetPlayerId].SpiedPlayers.Contains(ev.PlayerId))
            {
                events.Add(new WorldSpyRemovedEvent(ev.TargetPlayerId, ev.PlayerId));
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }
    }
}
