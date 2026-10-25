namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record PurchaseLeaderAction
    {
        private static class Considerations
        {
            private const int AffordabilityFloor = MaxScore / 10;
            private const int RepetitionCostMultiplier = 3;

            private static readonly UtilityScore.ResponseCurve AffordabilityCurve =
                new UtilityScore.ResponseCurve.Polynomial(Slope: MaxScore - AffordabilityFloor, Exponent: 1, XShift: 0, YShift: AffordabilityFloor);

            public static UtilityScore.Consideration Affordability(UnitDefinition leader, WorldPlayer player)
            {
                return UtilityScore.Ratio(player.Resources.Gold, leader.PurchaseCost.Gold, AffordabilityCurve);
            }

            public static UtilityScore.Consideration Repetition(UnitDefinition leader, WorldPlayer player, int ownedStacks)
            {
                if (ownedStacks == 0)
                {
                    return UtilityScore.Full;
                }
                else
                {
                    return UtilityScore.Flag(player.Resources.Gold >= leader.PurchaseCost.Gold * RepetitionCostMultiplier);
                }
            }
        }
    }
}
