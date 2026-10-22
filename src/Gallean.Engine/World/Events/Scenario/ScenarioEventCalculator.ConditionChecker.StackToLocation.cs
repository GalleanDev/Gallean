namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker
    {
        private sealed class StackToLocation(ScenarioEventStackToLocationCondition condition) :
            IScenarioEventTriggerVisitor<ScenarioEventConditionContext, bool>
        {
            public bool Visit(ScenarioEventTurnStartedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckAtLocation(context);
            }

            public bool Visit(ScenarioEventStackMovedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckAtLocation(context);
            }

            public bool Visit(ScenarioEventStackCreatedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckAtLocation(context);
            }

            public bool Visit(ScenarioEventStackDestroyedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckAtLocation(context);
            }

            public bool Visit(ScenarioEventFortificationCapturedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckAtLocation(context);
            }

            public bool Visit(ScenarioEventItemGainedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckAtLocation(context);
            }

            private bool CheckAtLocation(ScenarioEventConditionContext context)
            {
                return context.TriggererStack is not null && IsReferenceAt(context);
            }

            private bool IsReferenceAt(ScenarioEventConditionContext context)
            {
                return condition.Stack switch
                {
                    ScenarioStackReference.Stack stack => IsStackAt(stack.StackId, condition.Location, context),
                    ScenarioStackReference.Template => AnyPlayerReferencedStackAt(context.TriggererId, condition.Stack, condition.Location, context),
                    _ => throw new ArgumentOutOfRangeException(nameof(condition))
                };
            }
        }
    }
}
