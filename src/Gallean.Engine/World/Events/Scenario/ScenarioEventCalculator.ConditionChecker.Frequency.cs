namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        private static bool CheckFrequency(ScenarioEventFrequencyCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger switch
            {
                ScenarioEventTurnStartedTrigger turnStarted => IsFrequencyTurn(condition, turnStarted, context),
                ScenarioEventStackMovedTrigger => false,
                ScenarioEventStackCreatedTrigger => false,
                ScenarioEventStackDestroyedTrigger => false,
                ScenarioEventFortificationCapturedTrigger => false,
                ScenarioEventItemGainedTrigger => false
            };
        }

        private static bool IsFrequencyTurn(
            ScenarioEventFrequencyCondition condition,
            ScenarioEventTurnStartedTrigger trigger,
            ScenarioEventConditionContext context)
        {
            return trigger.PlayerId == context.TriggererId &&
                   condition.Frequency > 0 &&
                   (context.State.TurnInfo.TurnNumber % condition.Frequency) == 0;
        }
    }
}
