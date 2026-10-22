namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class TurnStartingStackExpirationRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldTurnStartingEvent ev, WorldState state)
        {
            List<WorldEvent> events = [];

            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (stack.OwnerId == ev.PlayerId && stack.ExpirationTurn <= state.TurnInfo.TurnNumber)
                {
                    events.Add(new WorldStackDestroyedEvent(stack.Id, Destroyer: null, DestroyerPlayerId: null));
                }
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }
    }
}
