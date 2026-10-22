namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker
    {
        private sealed class Frequency(ScenarioEventFrequencyCondition condition) :
            IScenarioEventTriggerVisitor<ScenarioEventConditionContext, bool>
        {
            public bool Visit(ScenarioEventTurnStartedTrigger trigger, ScenarioEventConditionContext context)
            {
                return trigger.PlayerId == context.TriggererId &&
                       condition.Frequency > 0 &&
                       (context.State.TurnInfo.TurnNumber % condition.Frequency) == 0;
            }

            public bool Visit(ScenarioEventStackMovedTrigger trigger, ScenarioEventConditionContext context) => false;
            public bool Visit(ScenarioEventStackCreatedTrigger trigger, ScenarioEventConditionContext context) => false;
            public bool Visit(ScenarioEventStackDestroyedTrigger trigger, ScenarioEventConditionContext context) => false;
            public bool Visit(ScenarioEventFortificationCapturedTrigger trigger, ScenarioEventConditionContext context) => false;
            public bool Visit(ScenarioEventItemGainedTrigger trigger, ScenarioEventConditionContext context) => false;
        }
    }
}
