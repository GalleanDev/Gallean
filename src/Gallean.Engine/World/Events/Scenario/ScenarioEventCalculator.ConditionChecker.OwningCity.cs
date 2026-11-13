namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        private static bool CheckFortificationOwned(ScenarioEventOwningCityCondition condition, ScenarioEventConditionContext context)
        {
            if (!context.State.Fortifications.TryGetValue(condition.Fortification, out WorldFortification? fortification))
            {
                return false;
            }

            return fortification switch
            {
                WorldCapital capital => capital.CapturedById is not null && IsAffectedRace(capital.CapturedById.Value, context),
                WorldCity city => IsAffectedRace(city.OwnerId, context),
                WorldRuin => false
            };
        }
    }
}
