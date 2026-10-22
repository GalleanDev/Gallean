namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class TurnStartingRestoreMovementRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldTurnStartingEvent ev, WorldState state)
        {
            List<WorldEvent> events = [];

            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (stack.OwnerId == ev.PlayerId)
                {
                    StackStats stats = Context.UnitStats.GetWorldStackStats(stack);
                    events.Add(new WorldStackMovementRestoredEvent(stack.Id, stats.MaxMovementPoints));
                }
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }
    }
}
