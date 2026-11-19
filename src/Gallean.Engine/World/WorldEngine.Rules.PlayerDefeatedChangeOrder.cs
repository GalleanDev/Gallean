using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class PlayerDefeatedChangeOrderRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldPlayerDefeatedEvent ev, WorldState state)
        {
            List<WorldEvent> events = [];

            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (stack.OwnerId == ev.PlayerId && stack.Order is not WorldStackOrder.Berserk)
                {
                    events.Add(new WorldStackOrderChangedEvent(stack.Id, WorldStackOrder.Berserk.Instance));
                }
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }
    }
}
