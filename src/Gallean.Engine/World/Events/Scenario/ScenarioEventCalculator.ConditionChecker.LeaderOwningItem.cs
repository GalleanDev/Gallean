namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker
    {
        private sealed class LeaderOwningItem(ScenarioEventLeaderOwningItemCondition condition) :
            IScenarioEventTriggerVisitor<ScenarioEventConditionContext, bool>
        {
            public bool Visit(ScenarioEventTurnStartedTrigger trigger, ScenarioEventConditionContext context)
            {
                return AnyReferencedStackWithItem(condition.Stack, condition.Item, context);
            }

            public bool Visit(ScenarioEventStackMovedTrigger trigger, ScenarioEventConditionContext context)
            {
                return AnyReferencedStackWithItem(condition.Stack, condition.Item, context);
            }

            public bool Visit(ScenarioEventStackCreatedTrigger trigger, ScenarioEventConditionContext context)
            {
                return AnyReferencedStackWithItem(condition.Stack, condition.Item, context);
            }

            public bool Visit(ScenarioEventStackDestroyedTrigger trigger, ScenarioEventConditionContext context)
            {
                return AnyReferencedStackWithItem(condition.Stack, condition.Item, context);
            }

            public bool Visit(ScenarioEventFortificationCapturedTrigger trigger, ScenarioEventConditionContext context)
            {
                return AnyReferencedStackWithItem(condition.Stack, condition.Item, context);
            }

            public bool Visit(ScenarioEventItemGainedTrigger trigger, ScenarioEventConditionContext context)
            {
                return AnyReferencedStackWithItem(condition.Stack, condition.Item, context);
            }
        }
    }
}
