namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class TurnEndedEndRoundRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldTurnEndedEvent ev, WorldState state)
        {
            WorldPlayerId nextPlayerId = state.GetNextPlayerId(ev.PlayerId);
            WorldPlayerId firstPlayerId = state.GetFirstPlayerId();

            if (nextPlayerId == firstPlayerId)
            {
                return EventRuleResult<WorldEvent>.Append(new WorldRoundEndedEvent());
            }
            else
            {
                return EventRuleResult<WorldEvent>.Empty;
            }
        }
    }
}
