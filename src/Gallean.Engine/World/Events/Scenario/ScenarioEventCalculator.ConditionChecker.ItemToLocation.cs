namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker
    {
        private sealed class ItemToLocation(ScenarioEventItemToLocationCondition condition) :
            IScenarioEventTriggerVisitor<ScenarioEventConditionContext, bool>
        {
            public bool Visit(ScenarioEventTurnStartedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckItemAtLocation(context);
            }

            public bool Visit(ScenarioEventStackMovedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckItemAtLocation(context);
            }

            public bool Visit(ScenarioEventStackCreatedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckItemAtLocation(context);
            }

            public bool Visit(ScenarioEventStackDestroyedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckItemAtLocation(context);
            }

            public bool Visit(ScenarioEventFortificationCapturedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckItemAtLocation(context);
            }

            public bool Visit(ScenarioEventItemGainedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckItemAtLocation(context);
            }

            private bool CheckItemAtLocation(ScenarioEventConditionContext context)
            {
                return context.TriggererStack is not null &&
                       IsAffectedRace(context.TriggererStack.OwnerId, context) &&
                       IsStackAt(context.TriggererStack.Id, condition.Location, context) &&
                       HasItem(context.TriggererStack.Id, condition.Item, context);
            }
        }
    }
}
