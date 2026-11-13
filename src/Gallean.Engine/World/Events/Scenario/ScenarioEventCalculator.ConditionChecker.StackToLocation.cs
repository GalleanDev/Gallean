namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        private static bool CheckStackAtLocation(ScenarioEventStackToLocationCondition condition, ScenarioEventConditionContext context)
        {
            return context.TriggererStack is not null && IsReferenceAt(condition, context);
        }

        private static bool IsReferenceAt(ScenarioEventStackToLocationCondition condition, ScenarioEventConditionContext context)
        {
            return condition.Stack switch
            {
                ScenarioStackReference.Stack stack => IsStackAt(stack.StackId, condition.Location, context),
                ScenarioStackReference.Template => AnyPlayerReferencedStackAt(context.TriggererId, condition.Stack, condition.Location, context)
            };
        }
    }
}
