using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class PlayerDefeatedChangeOwnerRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldPlayerDefeatedEvent ev, WorldState state)
        {
            List<WorldStackId> stackIds = [];

            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (stack.OwnerId == ev.PlayerId)
                {
                    stackIds.Add(stack.Id);
                }
            }

            if (stackIds.Count == 0)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldPlayerId neutralPlayerId = WorldCommandHelpers.GetNeutralPlayerId(state);
            WorldSubraceId neutralSubraceId = WorldCommandHelpers.GetPlayerSubraceId(state, neutralPlayerId);
            List<WorldEvent> events = [];

            foreach (WorldStackId stackId in stackIds)
            {
                events.Add(new WorldStackOwnerChangedEvent(stackId, neutralPlayerId, neutralSubraceId, PlayAnimation: false));
                events.Add(new WorldStackOrderChangedEvent(stackId, WorldStackOrder.Berserk.Instance));
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }
    }
}
