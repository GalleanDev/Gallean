namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        private static bool CheckItemOwned(ScenarioEventOwningItemCondition condition, ScenarioEventConditionContext context)
        {
            return AnyAffectedRaceStackWithItem(condition.Item, context) ||
                   AnyAffectedRaceFortificationWithItem(condition.Item, context);
        }
    }
}
