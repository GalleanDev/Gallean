namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        private static bool CheckEnteredCity(ScenarioEventEnteringCityCondition condition, ScenarioEventConditionContext context)
        {
            if (!context.State.Fortifications.TryGetValue(condition.Fortification, out WorldFortification? fortification))
            {
                return false;
            }

            WorldPlayerId? ownerId = FortificationCalculator.GetOwner(fortification);

            return ownerId is not null &&
                   IsAffectedRace(ownerId.Value, context) &&
                   AnyStackAt(condition.Fortification, context);
        }
    }
}
