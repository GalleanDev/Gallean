namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        private static bool CheckItemAtLocation(ScenarioEventItemToLocationCondition condition, ScenarioEventConditionContext context)
        {
            return context.TriggererStack is not null &&
                   IsAffectedRace(context.TriggererStack.OwnerId, context) &&
                   IsStackAt(context.TriggererStack.Id, condition.Location, context) &&
                   HasItem(context.TriggererStack.Id, condition.Item, context);
        }
    }
}
