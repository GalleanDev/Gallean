namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        private static bool CheckRuinLooted(ScenarioEventLootingRuinCondition condition, ScenarioEventConditionContext context)
        {
            if (!context.State.Fortifications.TryGetValue(condition.Ruin, out WorldFortification? fortification))
            {
                return false;
            }

            if (fortification is not WorldRuin ruin)
            {
                return false;
            }

            return ruin.LooterId is not null && IsAffectedRace(ruin.LooterId.Value, context);
        }
    }
}
