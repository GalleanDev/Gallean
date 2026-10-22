namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class TurnStartingDiscoverSpiesRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldTurnStartingEvent ev, WorldState state)
        {
            List<WorldEvent> events = [];

            foreach (WorldPlayerId targetPlayerId in state.Players[ev.PlayerId].SpiedPlayers)
            {
                if (SpyCalculator.RollSpyDiscover(Context.Random, Context.Globals))
                {
                    events.Add(new WorldSpyRemovedEvent(ev.PlayerId, targetPlayerId));
                }
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }
    }
}
