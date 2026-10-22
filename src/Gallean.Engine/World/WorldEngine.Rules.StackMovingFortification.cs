namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class StackMovingFortificationRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldStackMovingEvent ev, WorldState state)
        {
            return GetCrossingEvents(ev.StackId, ev.SourcePosition, ev.TargetPosition, state);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldStackRelocatedEvent ev, WorldState state)
        {
            return GetCrossingEvents(ev.StackId, ev.SourcePosition, ev.TargetPosition, state);
        }

        private static EventRuleResult<WorldEvent> GetCrossingEvents(
            WorldStackId stackId,
            WorldPosition sourcePosition,
            WorldPosition targetPosition,
            WorldState state)
        {
            if (sourcePosition == targetPosition)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldPlan plan = WorldPlan.For(state);
            List<WorldEvent> events = [];

            WorldFortification? exitedFortification = plan.GetFortificationAt(sourcePosition);

            if (exitedFortification is not null)
            {
                events.Add(new WorldFortificationExitedEvent(stackId, exitedFortification.Id));
            }

            WorldFortification? enteredFortification = plan.GetFortificationAt(targetPosition);

            if (enteredFortification is not null)
            {
                events.Add(new WorldFortificationEnteredEvent(stackId, enteredFortification.Id));
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }
    }
}
