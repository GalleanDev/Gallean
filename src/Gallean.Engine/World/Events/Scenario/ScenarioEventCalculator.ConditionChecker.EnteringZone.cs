namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        private static bool CheckEnteredZone(ScenarioEventEnteringZoneCondition condition, ScenarioEventConditionContext context)
        {
            return context.TriggererStack is not null &&
                   IsAffectedRace(context.TriggererStack.OwnerId, context) &&
                   IsStackAt(context.TriggererStack.Id, condition.Zone, context);
        }
    }
}
