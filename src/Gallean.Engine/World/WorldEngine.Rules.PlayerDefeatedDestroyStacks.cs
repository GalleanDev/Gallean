namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class PlayerDefeatedDestroyStacksRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldPlayerDefeatedEvent ev, WorldState state)
        {
            List<WorldEvent> events = [];

            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (stack.OwnerId == ev.PlayerId)
                {
                    events.Add(new WorldStackDestroyedEvent(stack.Id, Destroyer: null, DestroyerPlayerId: null));
                }
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }
    }
}
