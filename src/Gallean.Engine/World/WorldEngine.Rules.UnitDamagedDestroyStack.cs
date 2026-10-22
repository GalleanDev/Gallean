namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class UnitDamagedDestroyStackRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldUnitDamagedEvent ev, WorldState state)
        {
            if (ev.Group is not WorldUnitGroup.Stack stackGroup)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            if (!state.Stacks.TryGetValue(stackGroup.StackId, out WorldStack? stack))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            if (stack.Units.HasAliveUnits())
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            return EventRuleResult<WorldEvent>.Append(new WorldStackDestroyedEvent(stackGroup.StackId, Destroyer: null, ev.SourcePlayerId));
        }
    }
}
