namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class StackMovingCaptureFortificationRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldStackMovingEvent ev, WorldState state)
        {
            WorldStack stack = state.Stacks[ev.StackId];
            WorldPlan plan = WorldPlan.For(state);
            WorldFortification? fortification = plan.GetFortificationAt(ev.TargetPosition);

            if (fortification is null)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            if (fortification.Garrison.HasAliveUnits())
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldPlayerId? owner = FortificationCalculator.GetOwner(fortification);

            if (owner is null)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            if (!DiplomacyCalculator.IsHostile(state, stack.OwnerId, owner.Value))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            if (!FortificationCalculator.CanCapture(stack, Context.Definitions))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            return EventRuleResult<WorldEvent>.Append(
                new WorldFortificationCapturingEvent(fortification.Id, owner.Value, stack.OwnerId, stack.Id));
        }
    }
}
