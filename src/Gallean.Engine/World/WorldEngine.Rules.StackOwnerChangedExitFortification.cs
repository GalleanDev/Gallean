namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class StackOwnerChangedExitFortificationRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldStackOwnerChangedEvent ev, WorldState state)
        {
            WorldStack stack = state.Stacks[ev.StackId];
            WorldPlan plan = WorldPlan.For(state);
            WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

            if (fortification is null || FortificationCalculator.GetOwner(fortification) == stack.OwnerId)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldPosition? exit = FortificationCalculator.FindExit(fortification, stack, state, Context.Definitions);

            if (exit is null)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldStackDirection direction = StackStepCalculator.GetDirection(stack.Position, exit.Value);

            return EventRuleResult<WorldEvent>.Append(new WorldStackRelocatedEvent(stack.Id, stack.Position, exit.Value, direction));
        }
    }
}
