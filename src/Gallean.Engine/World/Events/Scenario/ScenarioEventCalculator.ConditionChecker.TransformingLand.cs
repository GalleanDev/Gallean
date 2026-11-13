namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        private static bool CheckLandTransformed(ScenarioEventTransformingLandCondition condition, ScenarioEventConditionContext context)
        {
            foreach (WorldRace race in context.AffectedRaces)
            {
                if (!WorldHelpers.IsRacePlayable(race) || !IsRacePlaying(race, context))
                {
                    continue;
                }

                WorldMapTerrain terrain = WorldHelpers.GetNativeTerrain(race);
                int transformedPercentage = TerrainTransformCalculator.GetTransformedPercentage(context.State.Map, terrain);

                if (transformedPercentage >= condition.LandPercentage)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
