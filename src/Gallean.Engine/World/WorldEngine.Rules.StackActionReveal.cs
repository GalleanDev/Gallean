namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class StackActionRevealRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldStackMovingEvent ev, WorldState state)
        {
            return Reveal(state, ev.StackId);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldStackBattleStartedEvent ev, WorldState state)
        {
            return Reveal(state, ev.AttackerStackId);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldFortificationBattleStartedEvent ev, WorldState state)
        {
            return Reveal(state, ev.AttackerStackId);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldItemUsedEvent ev, WorldState state)
        {
            return Reveal(state, ev.StackId);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldThiefActionSucceededEvent ev, WorldState state)
        {
            return Reveal(state, ev.ThiefStackId);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldThiefActionFailedEvent ev, WorldState state)
        {
            return Reveal(state, ev.ThiefStackId);
        }

        private static EventRuleResult<WorldEvent> Reveal(WorldState state, WorldStackId stackId)
        {
            if (!state.Stacks.TryGetValue(stackId, out WorldStack? stack))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            if (stack.Concealed)
            {
                return EventRuleResult<WorldEvent>.Append(new WorldStackRevealedEvent(stackId));
            }
            else
            {
                return EventRuleResult<WorldEvent>.Empty;
            }
        }
    }
}
