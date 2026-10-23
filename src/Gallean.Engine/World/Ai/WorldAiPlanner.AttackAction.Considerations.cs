namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record AttackAction
    {
        private static class Considerations
        {
            private const int PrefilterPowerPercent = 80;
            private const int RoamingPowerPercent = 140;
            private const int VitalPowerPercent = 140;
            private const int ExperienceFloor = MaxScore / 5;
            private const int ExperienceReference = 200;
            private const int ExperienceTradeRatio = 3;
            private const int HitpointsRecoveryPenalty = MaxScore / 10;
            private const int LeaderLostScore = MaxScore / 10;
            private const int UnfavorableTradeScore = MaxScore / 4;
            private const int UnitRecoveryPenalty = MaxScore * 3 / 10;
            private const int ScenarioObjectivePenalty = MaxScore / 2;
            private const int UnitsLostDivisor = 4;
            private const int ProximityDistancePenalty = 3;
            private const int ProximityCloseDistance = 7;
            private const int ProximityCloseMultiplier = 2;
            private const int ProximityLimit = 250;
            private const int SummonAttackRange = 5;
            private const int SummonDistancePenaltyPercent = 16;

            private static readonly UtilityScore.ResponseCurve ExperienceCurve =
                new UtilityScore.ResponseCurve.Polynomial(
                    Slope: MaxScore * MaxScore / ExperienceReference, Exponent: 1, XShift: 0, YShift: ExperienceFloor);

            private static readonly UtilityScore.ResponseCurve HitpointsRecoveryCurve =
                new UtilityScore.ResponseCurve.Polynomial(
                    Slope: HitpointsRecoveryPenalty, Exponent: 1, XShift: 0, YShift: MaxScore - HitpointsRecoveryPenalty);

            private static readonly UtilityScore.ResponseCurve UnitRecoveryCurve =
                new UtilityScore.ResponseCurve.Polynomial(
                    Slope: UnitRecoveryPenalty, Exponent: 1, XShift: 0, YShift: MaxScore - UnitRecoveryPenalty);

            public static UtilityScore.Consideration PowerFilter(WorldAiEstimator.PowerEstimate power)
            {
                return UtilityScore.Flag(power.AtLeast(BattleSide.Attacker, RoamingPowerPercent));
            }

            public static UtilityScore.Consideration PowerPrefilter(WorldAiEstimator.PowerEstimate power)
            {
                return UtilityScore.Flag(power.AtLeast(BattleSide.Attacker, PrefilterPowerPercent));
            }

            public static UtilityScore.Consideration PowerRatio(WorldAiEstimator.PowerEstimate power)
            {
                return UtilityScore.Ratio(power.Attacker, power.Attacker + power.Defender);
            }

            public static UtilityScore.Consideration Proximity(int interest, int distance)
            {
                int score = interest - (distance * ProximityDistancePenalty);

                if (distance < ProximityCloseDistance)
                {
                    return UtilityScore.Ratio(score * ProximityCloseMultiplier, ProximityLimit);
                }
                else
                {
                    return UtilityScore.Ratio(score, ProximityLimit);
                }
            }

            public static UtilityScore.Consideration SummonRange(int distance)
            {
                if (distance > SummonAttackRange)
                {
                    return UtilityScore.None;
                }
                else
                {
                    return UtilityScore.Ratio((SummonAttackRange + 1 - distance) * SummonDistancePenaltyPercent, 100);
                }
            }

            public static UtilityScore.Consideration ScenarioObjective(ActionContext context, WorldAiTarget target)
            {
                bool isObjective = context.ObjectiveTargets.IsEmpty || context.ObjectiveTargets.Contains(target);

                return UtilityScore.Flag(isObjective, ScenarioObjectivePenalty);
            }

            public static UtilityScore.Consideration Survival(WorldAiEstimator estimator, ActionContext context, BattleInfo battle)
            {
                if (context.IsStackVital)
                {
                    return UtilityScore.Flag(IsSafeBattle(battle));
                }
                else
                {
                    return UtilityScore.Full;
                }

                bool IsSafeBattle(BattleInfo battleInfo)
                {
                    if (battleInfo.Power.AtLeast(BattleSide.Attacker, VitalPowerPercent))
                    {
                        WorldAiEstimator.BattleEstimate estimate = estimator.EstimateBattle(battleInfo.BattleState);

                        return estimate is { IsWinner: true, IsLeaderAlive: true };
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public static UtilityScore.Consideration Victory(WorldAiEstimator.BattleEstimate estimate)
            {
                return UtilityScore.Flag(estimate.IsWinner);
            }

            public static UtilityScore.Consideration TradeExperience(WorldAiEstimator.BattleEstimate estimate)
            {
                bool isFavorable = estimate.ExperienceLost <= 0 || estimate.ExperienceGained >= estimate.ExperienceLost * ExperienceTradeRatio;

                return UtilityScore.Flag(isFavorable, UnfavorableTradeScore);
            }

            public static UtilityScore.Consideration TradeUnits(WorldAiEstimator.BattleEstimate estimate)
            {
                bool isFavorable = estimate.ExperienceLost <= 0 || estimate.UnitsLost * UnitsLostDivisor <= estimate.UnitCountBefore;

                return UtilityScore.Flag(isFavorable, UnfavorableTradeScore);
            }

            public static UtilityScore.Consideration LeaderRetained(WorldAiEstimator.BattleEstimate estimate)
            {
                return UtilityScore.Flag(estimate.IsLeaderAlive, LeaderLostScore);
            }

            public static UtilityScore.Consideration ExperienceScore(WorldAiEstimator.BattleEstimate estimate)
            {
                return UtilityScore.Of(estimate.ExperienceGained, ExperienceCurve);
            }

            public static UtilityScore.Consideration ExperienceRetained(WorldAiEstimator.BattleEstimate estimate)
            {
                return UtilityScore.InverseRatio(estimate.ExperienceLost, estimate.ExperienceBefore);
            }

            public static UtilityScore.Consideration HitpointsRetained(WorldAiEstimator.BattleEstimate estimate)
            {
                return UtilityScore.Ratio(estimate.HitpointsAfter, estimate.HitpointsBefore);
            }

            public static UtilityScore.Consideration UnitsRetained(WorldAiEstimator.BattleEstimate estimate)
            {
                return UtilityScore.Ratio(estimate.UnitCountAfter, estimate.UnitCountBefore);
            }

            public static UtilityScore.Consideration HitpointsRecoverability(WorldAiEstimator.BattleEstimate estimate)
            {
                return UtilityScore.Ratio(estimate.HitpointsAfter, estimate.HitpointsBefore, HitpointsRecoveryCurve);
            }

            public static UtilityScore.Consideration UnitsRecoverability(WorldAiEstimator.BattleEstimate estimate)
            {
                return UtilityScore.Ratio(estimate.UnitCountAfter, estimate.UnitCountBefore, UnitRecoveryCurve);
            }
        }
    }
}
