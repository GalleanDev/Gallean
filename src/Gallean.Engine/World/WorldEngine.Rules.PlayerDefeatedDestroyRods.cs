namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class PlayerDefeatedDestroyRodsRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldPlayerDefeatedEvent ev, WorldState state)
        {
            List<WorldEvent> events = [];

            foreach (WorldRod rod in state.Rods.Values)
            {
                if (rod.OwnerId == ev.PlayerId)
                {
                    events.Add(new WorldRodDestroyedEvent(rod.Id, PlayerId: null, rod.OwnerId, rod.Position));
                }
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }
    }
}
