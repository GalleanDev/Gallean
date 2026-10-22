namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record PurchaseUnitAction
    {
        private static class Considerations
        {
            private const int AllReachScore = MaxScore * 9 / 10;
            private const int AnyReachScore = MaxScore * 4 / 5;
            private const int AffordabilityPenalty = MaxScore / 2;
            private const int RepetitionPenalty = MaxScore * 3 / 4;

            private static readonly UtilityScore.ResponseCurve AffordabilityCurve =
                new UtilityScore.ResponseCurve.Polynomial(Slope: -AffordabilityPenalty, Exponent: 1, XShift: 0, YShift: MaxScore);

            private static readonly UtilityScore.ResponseCurve RepetitionCurve =
                new UtilityScore.ResponseCurve.Polynomial(Slope: -RepetitionPenalty, Exponent: 1, XShift: 0, YShift: MaxScore);

            public static UtilityScore.Consideration Affordability(UnitDefinition unit, WorldPlayer player)
            {
                return UtilityScore.Ratio(unit.PurchaseCost.Gold, player.Resources.Gold, AffordabilityCurve);
            }

            public static UtilityScore.Consideration ReachPreference(UnitDefinition unit)
            {
                return unit.Attack.Reach switch
                {
                    UnitAttackReach.Adjacent => UtilityScore.Full,
                    UnitAttackReach.All => UtilityScore.Of(AllReachScore),
                    UnitAttackReach.Any => UtilityScore.Of(AnyReachScore)
                };
            }

            public static UtilityScore.Consideration Repetition(UnitDefinition unit, List<UnitDefinitionId> composition)
            {
                int owned = 0;

                foreach (UnitDefinitionId unitId in composition)
                {
                    if (unitId == unit.Id)
                    {
                        owned++;
                    }
                }

                return UtilityScore.Ratio(owned, composition.Count, RepetitionCurve);
            }
        }
    }
}
