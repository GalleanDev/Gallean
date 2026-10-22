namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record MoveAction
    {
        private static class Considerations
        {
            private const int MaxInterest = 200;
            private const int MaxPriority = 6;
            private const int PrefilterPowerPercent = 80;
            private const int VitalPowerPercent = 140;
            private const int VitalThreatRadius = 3;
            private const int VitalThreatPowerPercent = 140;
            private const int ScenarioObjectivePenalty = MaxScore / 2;

            public static UtilityScore.Consideration TargetInterest(int interest)
            {
                return UtilityScore.Ratio(interest, MaxInterest);
            }

            public static UtilityScore.Consideration TargetPriority(int priority)
            {
                return UtilityScore.Ratio(priority, MaxPriority);
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

            public static UtilityScore.Consideration TargetMoveCost(int cost, int limit)
            {
                return UtilityScore.InverseRatio(cost, limit);
            }

            public static UtilityScore.Consideration PowerPrefilter(BattleInfo? battleInfo)
            {
                if (battleInfo is null)
                {
                    return UtilityScore.Full;
                }
                else
                {
                    return UtilityScore.Flag(battleInfo.Power.AtLeast(BattleSide.Attacker, PrefilterPowerPercent));
                }
            }

            public static UtilityScore.Consideration TargetSurvival(WorldAiEstimator estimator, ActionContext context, BattleInfo? battleInfo)
            {
                if (context.IsStackVital && battleInfo is not null)
                {
                    return UtilityScore.Flag(IsSafeBattle(battleInfo));
                }
                else
                {
                    return UtilityScore.Full;
                }

                bool IsSafeBattle(BattleInfo battle)
                {
                    if (battle.Power.AtLeast(BattleSide.Attacker, VitalPowerPercent))
                    {
                        WorldAiEstimator.BattleEstimate estimate = estimator.EstimateBattle(battle.BattleState);

                        return estimate is { IsWinner: true, IsLeaderAlive: true };
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public static UtilityScore.Consideration VitalStackSurvival(
                WorldAiContext aiContext,
                ActionContext context,
                WorldPathMap pathMap,
                WorldAiPathfinding.TargetPath targetPath)
            {
                if (context.IsStackVital)
                {
                    WorldPosition? movePosition = aiContext.Pathfinding.FindMoveTowardCell(context.State, context.Stack, pathMap, targetPath.Position);

                    if (movePosition is not null)
                    {
                        WorldAiMoveStackCommand command = new(context.Stack.Id, movePosition.Value);
                        WorldState? newState = aiContext.Estimator.EstimateCommand(context.State, command);

                        if (newState is not null && newState.Stacks.TryGetValue(context.Stack.Id, out WorldStack? stack))
                        {
                            bool hasThreat = CheckVitalThreat(aiContext, newState, stack);

                            return UtilityScore.Flag(!hasThreat);
                        }
                    }
                }

                return UtilityScore.Full;
            }

            private static bool CheckVitalThreat(WorldAiContext aiContext, WorldState state, WorldStack stack)
            {
                foreach (WorldStack other in state.Stacks.Values)
                {
                    if (DiplomacyCalculator.IsHostile(state, stack.OwnerId, other.OwnerId) &&
                        other.Position.GetDistance(stack.Position) <= VitalThreatRadius)
                    {
                        PathTraversalCalculator.Encounter.StackBattle encounter = new(stack, true, false);
                        WorldAiTarget.Stack target = new(stack.Id);
                        BattleInfo? battleInfo = GetBattleInfo(aiContext, state, other, target, stack.Position, encounter);

                        if (battleInfo is not null)
                        {
                            if (!battleInfo.Power.AtLeast(BattleSide.Defender, VitalThreatPowerPercent))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }
    }
}
