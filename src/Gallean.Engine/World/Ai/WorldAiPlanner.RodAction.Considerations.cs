namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record RodAction
    {
        private static class Considerations
        {
            private const int MaxInterest = 200;

            public static UtilityScore.Consideration TargetInterest(int interest)
            {
                return UtilityScore.Ratio(interest, MaxInterest);
            }

            public static UtilityScore.Consideration TargetMoveCost(int cost, int limit)
            {
                return UtilityScore.InverseRatio(cost, limit);
            }
        }
    }
}
