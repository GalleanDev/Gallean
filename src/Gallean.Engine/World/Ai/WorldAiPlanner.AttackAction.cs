using static Gallean.Engine.WorldAiTargetEnumerator;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record AttackAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
    {
        private enum ScoringType
        {
            Ordered,
            Roaming,
            Prefiltered,
            Simulated,
            Summon
        }

        private const int BlockingRadius = 1;

        public override WorldAiActionPlan? Visit(WorldStackOrder.Normal order, ActionContext context)
        {
            return PlanNormalAttack(context, Enumerate(context.State, context.Stack.OwnerId, TargetType.Enemy | TargetType.Ruin, area: null));
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Stand order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Guard order, ActionContext context)
        {
            WorldState state = context.State;
            TargetArea area = new(context.Stack.Position, Context.Engine.Globals.AiStackGuardRange / 2);
            List<WorldAiTarget> targets = [];

            foreach (WorldAiTarget target in Enumerate(state, context.Stack.OwnerId, TargetType.EnemyStack, area))
            {
                if (target is WorldAiTarget.Stack stackTarget &&
                    state.Stacks[stackTarget.StackId].Order is not WorldStackOrder.Stand)
                {
                    targets.Add(target);
                }
            }

            return PlanAttack(context, targets, ScoringType.Ordered);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.AttackStack order, ActionContext context)
        {
            if (context.State.Stacks.TryGetValue(order.TargetId, out WorldStack? target) && !target.Concealed)
            {
                return PlanAttack(context, [new WorldAiTarget.Stack(order.TargetId)], ScoringType.Ordered);
            }
            else
            {
                return null;
            }
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendStack order, ActionContext context)
        {
            if (!context.State.Stacks.TryGetValue(order.TargetId, out WorldStack? protectedStack))
            {
                return null;
            }

            TargetArea area = new(protectedStack.Position, Context.Engine.Globals.AiStackDefendRange / 2);
            IEnumerable<WorldAiTarget> targets = Enumerate(context.State, context.Stack.OwnerId, TargetType.Enemy | TargetType.Ruin, area);

            return PlanAttack(context, targets, ScoringType.Ordered);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.SecureCity order, ActionContext context)
        {
            IEnumerable<WorldAiTarget> targets = Enumerate(context.State, context.Stack.OwnerId, TargetType.Enemy, BlockingArea(context));

            return PlanAttack(context, targets, ScoringType.Ordered);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Roam order, ActionContext context)
        {
            WorldPlan plan = WorldPlan.For(context.State);
            WorldFortification? fortification = plan.GetFortificationAt(context.Stack.Position);

            if (fortification is not (WorldCity or WorldCapital { CapturedById: null }))
            {
                return PlanAttack(context, Enumerate(context.State, context.Stack.OwnerId, TargetType.Enemy, area: null), ScoringType.Roaming);
            }
            else
            {
                return null;
            }
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Explore order, ActionContext context)
        {
            IEnumerable<WorldAiTarget> targets = Enumerate(context.State, context.Stack.OwnerId, TargetType.Enemy | TargetType.Ruin, BlockingArea(context));

            return PlanNormalAttack(context, targets);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.MoveToLocation order, ActionContext context)
        {
            IEnumerable<WorldAiTarget> targets = Enumerate(context.State, context.Stack.OwnerId, TargetType.Enemy | TargetType.Ruin, BlockingArea(context));

            return PlanAttack(context, targets, ScoringType.Ordered);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendLocation order, ActionContext context)
        {
            TargetArea area = new(order.TargetPosition, order.Radius);
            IEnumerable<WorldAiTarget> targets = Enumerate(context.State, context.Stack.OwnerId, TargetType.EnemyStack, area);

            return PlanAttack(context, targets, ScoringType.Ordered);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Berserk order, ActionContext context)
        {
            return PlanAttack(context, Enumerate(context.State, context.Stack.OwnerId, TargetType.Enemy, area: null), ScoringType.Ordered);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Assist order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Steal order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendCity order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Heal order, ActionContext context)
        {
            IEnumerable<WorldAiTarget> targets = Enumerate(context.State, context.Stack.OwnerId, TargetType.Enemy | TargetType.Ruin, BlockingArea(context));

            return PlanNormalAttack(context, targets);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Reinforce order, ActionContext context)
        {
            return PlanNormalAttack(context, Enumerate(context.State, context.Stack.OwnerId, TargetType.Enemy | TargetType.Ruin, area: null));
        }

        private WorldAiActionPlan? PlanNormalAttack(ActionContext context, IEnumerable<WorldAiTarget> targets)
        {
            if (ThiefCalculator.IsThief(context.Stack, Context.Engine.Definitions))
            {
                return null;
            }

            WorldPlayer owner = context.State.Players[context.Stack.OwnerId];
            ScoringType scoring;

            if (IsSummon(context.Stack))
            {
                scoring = ScoringType.Summon;
            }
            else if (!WorldHelpers.IsRacePlayable(owner.Race))
            {
                scoring = ScoringType.Roaming;
            }
            else if (context.State.Difficulty is WorldDifficulty.Hard or WorldDifficulty.VeryHard)
            {
                scoring = ScoringType.Simulated;
            }
            else
            {
                scoring = ScoringType.Prefiltered;
            }

            return PlanAttack(context, targets, scoring);
        }

        private WorldAiActionPlan? PlanAttack(ActionContext context, IEnumerable<WorldAiTarget> targets, ScoringType scoring)
        {
            (int Score, BattleInfo Battle)? best = null;

            foreach (BattleInfo battle in EnumerateBattles(context, targets))
            {
                int score = ScoreBattle(context, battle, scoring);

                if (score > 0)
                {
                    if (best is null || score > best.Value.Score)
                    {
                        best = (score, battle);
                    }
                }
            }

            if (best is null)
            {
                return null;
            }

            return MakeAttackPlan(context, best.Value.Battle, best.Value.Score);
        }

        private WorldAiActionPlan MakeAttackPlan(ActionContext context, BattleInfo battle, int score)
        {
            WorldAiStackMemory stackMemory = context.Memory.GetStackMemory(context.Stack);
            WorldAiMemory memory = context.Memory.WithStackMemory(context.Stack.Id, stackMemory with { Target = battle.Target });

            return MakePlan(score, new WorldAiMoveStackCommand(context.Stack.Id, battle.TargetPosition), memory);
        }

        private int ScoreBattle(ActionContext context, BattleInfo battle, ScoringType scoring)
        {
            return scoring switch
            {
                ScoringType.Ordered => UtilityScore.Combine(
                [
                    Considerations.PowerRatio(battle.Power),
                    Considerations.ScenarioObjective(context, battle.Target),
                    Considerations.Survival(Context.Estimator, context, battle)
                ]),
                ScoringType.Roaming => ScoreRoamingBattle(context, battle),
                ScoringType.Prefiltered => ScorePrefilteredBattle(context, battle),
                ScoringType.Simulated => ScoreSimulatedBattle(context, battle),
                ScoringType.Summon => ScoreSummonBattle(context, battle)
            };
        }

        private int ScoreSummonBattle(ActionContext context, BattleInfo battle)
        {
            int distance = context.Stack.Position.GetDistance(battle.TargetPosition);

            return UtilityScore.Combine(
            [
                Considerations.SummonRange(distance),
                Considerations.PowerRatio(battle.Power),
                Considerations.ScenarioObjective(context, battle.Target),
                Considerations.Survival(Context.Estimator, context, battle)
            ]);
        }

        private int ScoreRoamingBattle(ActionContext context, BattleInfo battle)
        {
            WorldState state = context.State;
            WorldPlayer player = state.Players[context.Stack.OwnerId];
            WorldAiAttitudeDefinition attitude = Context.Engine.Definitions.GetAiAttitude(player.AiAttitude);
            WorldAiTargetInterestVisitor interestVisitor = new(attitude);
            int interest = battle.Target.Accept(interestVisitor, new WorldAiTargetInterestVisitor.Context(state, context.Stack.OwnerId));
            int distance = context.Stack.Position.GetDistance(battle.TargetPosition);

            return UtilityScore.Combine(
            [
                Considerations.PowerFilter(battle.Power),
                Considerations.PowerRatio(battle.Power),
                Considerations.Proximity(interest, distance),
                Considerations.ScenarioObjective(context, battle.Target),
                Considerations.Survival(Context.Estimator, context, battle)
            ]);
        }

        private int ScorePrefilteredBattle(ActionContext context, BattleInfo battle)
        {
            UtilityScore.Consideration prefilter = Considerations.PowerPrefilter(battle.Power);

            if (prefilter.Score > 0)
            {
                return UtilityScore.Combine(
                [
                    Considerations.PowerRatio(battle.Power),
                    Considerations.ScenarioObjective(context, battle.Target),
                    Considerations.Survival(Context.Estimator, context, battle)
                ]);
            }
            else
            {
                Context.Trace.SkipStackBattle(context.Stack, battle.TargetPosition, battle.BattlePosition, battle.Power);

                return 0;
            }
        }

        private int ScoreSimulatedBattle(ActionContext context, BattleInfo battle)
        {
            UtilityScore.Consideration prefilter = Considerations.PowerPrefilter(battle.Power);

            if (prefilter.Score > 0)
            {
                WorldAiEstimator.BattleEstimate estimate = Context.Estimator.EstimateBattle(battle.BattleState);

                int score = UtilityScore.Combine(
                [
                    Considerations.Victory(estimate),
                    Considerations.TradeExperience(estimate),
                    Considerations.TradeUnits(estimate),
                    Considerations.LeaderRetained(estimate),
                    Considerations.ExperienceScore(estimate),
                    Considerations.ExperienceRetained(estimate),
                    Considerations.HitpointsRetained(estimate),
                    Considerations.UnitsRetained(estimate),
                    Considerations.HitpointsRecoverability(estimate),
                    Considerations.UnitsRecoverability(estimate),
                    Considerations.ScenarioObjective(context, battle.Target),
                    Considerations.Survival(Context.Estimator, context, battle)
                ]);

                Context.Trace.EstimateStackBattle(context.Stack, battle.TargetPosition, battle.BattlePosition, estimate, score);

                return score;
            }
            else
            {
                Context.Trace.SkipStackBattle(context.Stack, battle.TargetPosition, battle.BattlePosition, battle.Power);

                return 0;
            }
        }

        private IEnumerable<BattleInfo> EnumerateBattles(ActionContext context, IEnumerable<WorldAiTarget> targets)
        {
            WorldStack stack = context.Stack;

            if (stack.MovementPoints <= 0)
            {
                yield break;
            }

            WorldPathMap? pathMap = Context.Pathfinding.GetPathMap(context.State, stack);

            if (pathMap is null)
            {
                yield break;
            }

            IReadOnlyList<WorldAiPathfinding.TargetPath> targetPaths = Context.Pathfinding.GetTargetPaths(context.State, pathMap, targets);

            foreach (WorldAiPathfinding.TargetPath targetPath in targetPaths)
            {
                WorldAiMoveStackCommand moveCommand = new(stack.Id, targetPath.Position);
                WorldCommandResult moveResult = moveCommand.Execute(context.State, Context.Engine);

                if (!moveResult.Failed)
                {
                    PathTraversalCalculator.TraversalInfo traversal = Context.Pathfinding.GetTraversalInfo(context.State, stack, targetPath.Path);

                    if (traversal.Encounter is PathTraversalCalculator.Encounter.Battle { CanBegin: true, CanAvoid: false })
                    {
                        BattleInfo? battleInfo = GetBattleInfo(
                            Context, context.State, stack, targetPath.Target, targetPath.Position, traversal.Encounter);

                        if (battleInfo is not null)
                        {
                            yield return battleInfo;
                        }
                    }
                }
            }
        }

        private static TargetArea BlockingArea(ActionContext context)
        {
            return new TargetArea(context.Stack.Position, BlockingRadius);
        }

        private bool IsSummon(WorldStack stack)
        {
            (_, _, UnitDefinition leaderDefinition) = stack.GetLeader(Context.Engine.Definitions);

            return leaderDefinition.Category is UnitCategory.Summon;
        }
    }
}
