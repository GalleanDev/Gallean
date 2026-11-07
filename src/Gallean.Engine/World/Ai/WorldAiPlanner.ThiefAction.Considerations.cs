namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record ThiefAction
    {
        private static class Considerations
        {
            private const int MaxInterest = 200;
            private const int MaxPriority = 6;
            private const int ChanceMax = 100;
            private const int MinimumSuccessChance = 50;
            private const int SpyValue = MaxScore / 2;
            private const int AssassinateValue = MaxScore / 2;
            private const int RiotValue = MaxScore / 2;
            private const int NeutralRiotValue = MaxScore / 10;
            private const int CounterfeitOrdersValue = MaxScore / 10;
            private const int SpyRuinValue = MaxScore / 10;
            private const int PoisonValue = MaxScore / 20;
            private const int GoldMaxValue = 1000;
            private const int DuelExperienceMaxValue = 200;
            private const int PercentMax = 100;

            public static UtilityScore.Consideration SuccessChance(int chance)
            {
                if (chance < MinimumSuccessChance)
                {
                    return UtilityScore.None;
                }
                else
                {
                    return UtilityScore.Ratio(chance, ChanceMax);
                }
            }

            public static UtilityScore.Consideration Difficulty(WorldThiefActionType type, WorldDifficulty difficulty, GlobalVariables globals)
            {
                if (IsExtendedAction(type))
                {
                    int percent = difficulty switch
                    {
                        WorldDifficulty.Easy => globals.AiThiefExtendedActionPercentEasy,
                        WorldDifficulty.Average => globals.AiThiefExtendedActionPercentAverage,
                        WorldDifficulty.Hard => globals.AiThiefExtendedActionPercentHard,
                        WorldDifficulty.VeryHard => globals.AiThiefExtendedActionPercentVeryHard
                    };

                    return UtilityScore.Ratio(percent, PercentMax);
                }
                else
                {
                    return UtilityScore.Full;
                }
            }

            private static bool IsExtendedAction(WorldThiefActionType type)
            {
                return type switch
                {
                    WorldThiefActionType.PoisonStack => false,
                    WorldThiefActionType.Spy => false,
                    WorldThiefActionType.StealItem => true,
                    WorldThiefActionType.Assassinate => true,
                    WorldThiefActionType.CounterfeitOrders => true,
                    WorldThiefActionType.Duel => false,
                    WorldThiefActionType.PoisonCity => false,
                    WorldThiefActionType.StealSpell => true,
                    WorldThiefActionType.Bribe => true,
                    WorldThiefActionType.StealGold => true,
                    WorldThiefActionType.StealMerchant => false,
                    WorldThiefActionType.StealMage => false,
                    WorldThiefActionType.SpyRuin => false,
                    WorldThiefActionType.RiotCity => true
                };
            }

            public static UtilityScore.Consideration TargetInterest(int interest)
            {
                return UtilityScore.Ratio(interest, MaxInterest);
            }

            public static UtilityScore.Consideration TargetPriority(int priority)
            {
                return UtilityScore.Ratio(priority, MaxPriority);
            }

            public static UtilityScore.Consideration TargetMoveCost(int cost, int limit)
            {
                return UtilityScore.InverseRatio(cost, limit);
            }

            public static UtilityScore.Consideration ScenarioBlockedTarget(ActionContext context, WorldAiTarget target)
            {
                return UtilityScore.Flag(!context.BlockedTargets.Contains(target));
            }

            public static UtilityScore.Consideration Spy(Approach approach)
            {
                if (approach.Target.OwnerId is not null &&
                    !SpyCalculator.SpiesOnPlayer(approach.State, approach.Thief.OwnerId, approach.Target.OwnerId.Value))
                {
                    return UtilityScore.Of(SpyValue);
                }
                else
                {
                    return UtilityScore.None;
                }
            }

            public static UtilityScore.Consideration SpyRuin()
            {
                return UtilityScore.Of(SpyRuinValue);
            }

            public static UtilityScore.Consideration Poison()
            {
                return UtilityScore.Of(PoisonValue);
            }

            public static UtilityScore.Consideration CounterfeitOrders(Approach approach, WorldAiContext context)
            {
                WorldUnitGroup? group = ThiefCalculator.GetUnitGroup(approach.Target);

                if (group is not null)
                {
                    UnitGrid<WorldUnit> units = WorldUnitGroupCalculator.GetUnits(approach.State, group);

                    foreach ((WorldUnit unit, GridPosition _) in units.GetUnits())
                    {
                        UnitDefinition definition = context.Engine.Definitions.GetUnit(unit.DefinitionId);

                        if (unit.IsAlive && definition.Attack.Reach is UnitAttackReach.Any or UnitAttackReach.All)
                        {
                            return UtilityScore.Of(CounterfeitOrdersValue);
                        }
                    }
                }

                return UtilityScore.None;
            }

            public static UtilityScore.Consideration StealItem(ItemDefinitionId itemId, WorldAiContext context)
            {
                ItemDefinition item = context.Engine.Definitions.GetItem(itemId);

                return UtilityScore.Ratio(item.Value.Gold, GoldMaxValue);
            }

            public static UtilityScore.Consideration Assassinate()
            {
                return UtilityScore.Of(AssassinateValue);
            }

            public static UtilityScore.Consideration Duel(Approach approach, WorldAiContext context)
            {
                if (approach.Target is ThiefStackTarget stackTarget)
                {
                    (WorldUnit leader, GridPosition _, UnitDefinition _) = stackTarget.Stack.GetLeader(context.Engine.Definitions);
                    UnitStats leaderStats = context.Engine.UnitStats.GetWorldUnitStats(leader);
                    BattleState battleState = WorldCommandHelpers.CreateDuelBattleState(approach.State, approach.Thief, stackTarget.Stack, context.Engine);
                    WorldAiEstimator.BattleEstimate estimate = context.Estimator.EstimateBattle(battleState);

                    if (estimate is { IsWinner: true, IsLeaderAlive: true })
                    {
                        return UtilityScore.Ratio(leaderStats.ExperienceKilled, DuelExperienceMaxValue);
                    }
                    else
                    {
                        return UtilityScore.None;
                    }
                }
                else
                {
                    return UtilityScore.None;
                }
            }

            public static UtilityScore.Consideration StealSpell(WorldSpellDefinitionId spellId, WorldAiContext context)
            {
                WorldSpellDefinition spell = context.Engine.Definitions.GetSpell(spellId);

                return UtilityScore.Ratio(spell.PurchaseCost.Gold, GoldMaxValue);
            }

            public static UtilityScore.Consideration Bribe(Approach approach, WorldAiContext context)
            {
                if (approach.Target is ThiefFortificationTarget { Fortification: WorldCity city })
                {
                    WorldResourceSet cost = ThiefCalculator.GetCityBribeCost(city, approach.State, context.Engine);
                    WorldPlayer player = approach.State.Players[approach.Thief.OwnerId];

                    return UtilityScore.InverseRatio(cost.Gold, player.Resources.Gold);
                }
                else
                {
                    return UtilityScore.None;
                }
            }

            public static UtilityScore.Consideration StealGold(Approach approach, WorldAiContext context)
            {
                if (approach.Target.OwnerId is not null)
                {
                    WorldPlayer owner = approach.State.Players[approach.Target.OwnerId.Value];
                    int maxGold = owner.Resources.Gold * context.Engine.Globals.StealGoldPlayerPercent / PercentMax;

                    return UtilityScore.Ratio(maxGold / 2, GoldMaxValue);
                }
                else
                {
                    return UtilityScore.None;
                }
            }

            public static UtilityScore.Consideration Riot(Approach approach)
            {
                if (approach.Target.OwnerId is not null)
                {
                    WorldPlayer owner = approach.State.Players[approach.Target.OwnerId.Value];

                    if (WorldHelpers.IsRacePlayable(owner.Race))
                    {
                        return UtilityScore.Of(RiotValue);
                    }
                    else
                    {
                        return UtilityScore.Of(NeutralRiotValue);
                    }
                }
                else
                {
                    return UtilityScore.None;
                }
            }
        }
    }
}
