namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record TakeBagAction
    {
        private static class Considerations
        {
            private const int NearDistance = 7;
            private const int NearMultiplier = 2;
            private const int DistancePenalty = 3;
            private const int FarPercent = 75;
            private const int InterestLimit = 250;
            private const int ScenarioObjectivePenalty = MaxScore / 2;

            public static UtilityScore.Consideration BagInterest(int interest, int distance)
            {
                if (distance < NearDistance)
                {
                    return UtilityScore.Ratio(interest * NearMultiplier, InterestLimit);
                }
                else
                {
                    return UtilityScore.Ratio((interest - (distance * DistancePenalty)) * FarPercent / 100, InterestLimit);
                }
            }

            public static UtilityScore.Consideration ScenarioObjectiveTarget(ActionContext context, WorldAiTarget target)
            {
                bool isObjective = context.ObjectiveTargets.IsEmpty || context.ObjectiveTargets.Contains(target);

                return UtilityScore.Flag(isObjective, ScenarioObjectivePenalty);
            }

            public static UtilityScore.Consideration ScenarioBlockedTarget(ActionContext context, WorldAiTarget target)
            {
                return UtilityScore.Flag(!context.BlockedTargets.Contains(target));
            }
        }
    }
}
