namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker
    {
        private sealed class TransformingLand(ScenarioEventTransformingLandCondition condition) :
            IScenarioEventTriggerVisitor<ScenarioEventConditionContext, bool>
        {
            public bool Visit(ScenarioEventTurnStartedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckLandTransformed(context);
            }

            public bool Visit(ScenarioEventStackMovedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckLandTransformed(context);
            }

            public bool Visit(ScenarioEventStackCreatedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckLandTransformed(context);
            }

            public bool Visit(ScenarioEventStackDestroyedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckLandTransformed(context);
            }

            public bool Visit(ScenarioEventFortificationCapturedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckLandTransformed(context);
            }

            public bool Visit(ScenarioEventItemGainedTrigger trigger, ScenarioEventConditionContext context)
            {
                return CheckLandTransformed(context);
            }

            private bool CheckLandTransformed(ScenarioEventConditionContext context)
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
}
