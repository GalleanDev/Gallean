using static Gallean.Engine.WorldAiTargetEnumerator;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record MoveAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
    {
        private enum ScoringType
        {
            Default,
            Ordered,
            Committed
        }

        private const int HoldDistance = 2;
        private const int RoamBagDistance = 3;
        private const int MoveTurnLimit = 10;

        public override WorldAiActionPlan? Visit(WorldStackOrder.Normal order, ActionContext context)
        {
            return PlanNormalMove(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Stand order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Guard order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.AttackStack order, ActionContext context)
        {
            if (context.State.Stacks.TryGetValue(order.TargetId, out WorldStack? target) && !target.Concealed)
            {
                return PlanMove(context, [new WorldAiTarget.Stack(order.TargetId)], ScoringType.Ordered);
            }
            else
            {
                return null;
            }
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendStack order, ActionContext context)
        {
            return PlanMove(context, [new WorldAiTarget.Stack(order.TargetId)], ScoringType.Ordered);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.SecureCity order, ActionContext context)
        {
            return PlanMove(context, [new WorldAiTarget.Fortification(order.TargetId)], ScoringType.Ordered);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Roam order, ActionContext context)
        {
            WorldState state = context.State;
            WorldStack stack = context.Stack;

            WorldPlan plan = WorldPlan.For(state);
            WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

            if (fortification is not (WorldCity or WorldCapital { CapturedById: null }))
            {
                IEnumerable<WorldAiTarget> targets = Enumerate(state, stack.OwnerId, TargetType.Enemy, area: null);

                if (CanPickUpBags(state, stack))
                {
                    TargetArea bagArea = new(stack.Position, RoamBagDistance);

                    targets = targets.Concat(Enumerate(state, stack.OwnerId, TargetType.Bag, bagArea));
                }

                return PlanMove(context, targets, ScoringType.Default);
            }
            else
            {
                return null;
            }
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Explore order, ActionContext context)
        {
            WorldStack stack = context.Stack;

            if (stack.MovementPoints <= 0)
            {
                return null;
            }

            WorldPathMap? pathMap = Context.Pathfinding.GetPathMap(context.State, stack);

            if (pathMap is null)
            {
                return null;
            }

            return PlanExploreMove(context, pathMap);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.MoveToLocation order, ActionContext context)
        {
            if (context.Stack.Position.GetDistance(order.TargetPosition) > order.Radius)
            {
                TargetArea area = new(order.TargetPosition, order.Radius);

                return PlanMove(context, EnumerateFreeTiles(context.State, context.Stack, area, Context.Engine.Definitions), ScoringType.Ordered);
            }
            else
            {
                return null;
            }
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendLocation order, ActionContext context)
        {
            if (context.Stack.Position.GetDistance(order.TargetPosition) > order.Radius)
            {
                TargetArea area = new(order.TargetPosition, order.Radius);

                return PlanMove(context, EnumerateFreeTiles(context.State, context.Stack, area, Context.Engine.Definitions), ScoringType.Ordered);
            }
            else
            {
                return null;
            }
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Berserk order, ActionContext context)
        {
            return PlanMove(context, Enumerate(context.State, context.Stack.OwnerId, TargetType.Enemy, area: null), ScoringType.Committed);
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
            IEnumerable<WorldAiTarget> targets = context.State.Fortifications.Values
                .Where(fortification => FortificationCalculator.GetOwner(fortification) == context.Stack.OwnerId)
                .Select(fortification => new WorldAiTarget.Fortification(fortification.Id));

            return PlanMove(context, targets, ScoringType.Ordered);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Reinforce order, ActionContext context)
        {
            return PlanMove(context, [new WorldAiTarget.Fortification(order.TargetId)], ScoringType.Ordered);
        }

        private WorldAiActionPlan? PlanNormalMove(ActionContext context)
        {
            WorldState state = context.State;
            WorldStack stack = context.Stack;

            if (stack.MovementPoints <= 0)
            {
                return null;
            }

            WorldPathMap? pathMap = Context.Pathfinding.GetPathMap(state, stack);

            if (pathMap is null)
            {
                return null;
            }

            TargetType targetType = TargetType.Enemy | TargetType.Ruin;

            if (CanPickUpBags(state, stack))
            {
                targetType |= TargetType.Bag;
            }

            IEnumerable<WorldAiTarget> targets = Enumerate(state, stack.OwnerId, targetType, area: null);

            IReadOnlyList<WorldAiPathfinding.TargetPath> paths = ScoreTargets(context, pathMap, targets, ScoringType.Default);

            if (paths.Count > 0)
            {
                return PlanScoredMove(context, pathMap, paths, ScoringType.Default);
            }
            else
            {
                return PlanExploreMove(context, pathMap);
            }
        }

        private WorldAiActionPlan? PlanMove(ActionContext context, IEnumerable<WorldAiTarget> targets, ScoringType scoring)
        {
            WorldState state = context.State;
            WorldStack stack = context.Stack;

            if (stack.MovementPoints <= 0)
            {
                return null;
            }

            WorldPathMap? pathMap = Context.Pathfinding.GetPathMap(state, stack);

            if (pathMap is null)
            {
                return null;
            }

            return PlanScoredMove(context, pathMap, ScoreTargets(context, pathMap, targets, scoring), scoring);
        }

        private WorldAiActionPlan? PlanScoredMove(
            ActionContext context,
            WorldPathMap pathMap,
            IReadOnlyList<WorldAiPathfinding.TargetPath> paths,
            ScoringType scoring)
        {
            WorldStack stack = context.Stack;

            foreach (WorldAiPathfinding.TargetPath path in paths)
            {
                WorldAiMemory memory = RememberTarget(context, path.Target, scoring);
                WorldAiActionPlan? plan = PlanMoveToward(context, pathMap, path.Position, memory);

                if (plan is not null)
                {
                    return plan;
                }

                if (stack.Position.GetDistance(path.Position) <= HoldDistance)
                {
                    return null;
                }
            }

            return null;
        }

        private WorldAiActionPlan? PlanExploreMove(ActionContext context, WorldPathMap pathMap)
        {
            WorldState state = context.State;
            WorldStack stack = context.Stack;
            WorldPlayer player = state.Players[stack.OwnerId];
            WorldAiStackMemory stackMemory = context.Memory.GetStackMemory(stack);

            if (stackMemory.Target is WorldAiTarget.Location exploreTarget)
            {
                if (ScoreFrontier(exploreTarget.Position) is not null)
                {
                    WorldAiActionPlan? plan = PlanMoveToward(context, pathMap, exploreTarget.Position, context.Memory);

                    if (plan is not null)
                    {
                        return plan;
                    }
                }
            }

            (int Score, WorldPosition Target)? best = null;

            for (int y = 0; y < state.Map.Height; y++)
            {
                for (int x = 0; x < state.Map.Width; x++)
                {
                    WorldPosition target = new(x, y);

                    int? score = ScoreFrontier(target);

                    if (score is not null && (best is null || score > best.Value.Score))
                    {
                        best = (score.Value, target);
                    }
                }
            }

            if (best is null)
            {
                return null;
            }

            WorldAiTarget.Location frontier = new(best.Value.Target);
            WorldAiMemory memory = context.Memory.WithStackMemory(stack.Id, stackMemory with { Target = frontier });

            return PlanMoveToward(context, pathMap, frontier.Position, memory);

            int? ScoreFrontier(WorldPosition target)
            {
                if (!state.Map.IsValidPosition(target) || !player.FogOfWar.Get(target.X, target.Y))
                {
                    return null;
                }

                int hiddenCount = CountAdjacentHiddenCells(state, player, target);

                if (hiddenCount <= 0)
                {
                    return null;
                }

                if (!pathMap.TryGetPath(target, WorldMovementLimit.Unlimited, out WorldPath? path))
                {
                    return null;
                }

                if (path.Positions.IsEmpty)
                {
                    return null;
                }

                PathTraversalCalculator.TraversalInfo traversal = Context.Pathfinding.GetTraversalInfo(state, stack, path);

                if (traversal.Encounter is PathTraversalCalculator.Encounter.Battle)
                {
                    return null;
                }

                return hiddenCount * 10 - path.Cost;
            }

            static int CountAdjacentHiddenCells(WorldState state, WorldPlayer player, WorldPosition position)
            {
                int count = 0;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx != 0 || dy != 0)
                        {
                            WorldPosition adjacent = new(position.X + dx, position.Y + dy);

                            if (state.Map.IsValidPosition(adjacent) && !player.FogOfWar.Get(adjacent.X, adjacent.Y))
                            {
                                count++;
                            }
                        }
                    }
                }

                return count;
            }
        }

        private WorldAiActionPlan? PlanMoveToward(ActionContext context, WorldPathMap pathMap, WorldPosition targetPosition, WorldAiMemory memory)
        {
            WorldStack stack = context.Stack;

            WorldPosition? movePosition = Context.Pathfinding.FindMoveTowardCell(context.State, stack, pathMap, targetPosition);

            if (movePosition is not null && pathMap.TryGetPath(movePosition.Value, WorldMovementLimit.Unlimited, out WorldPath? path))
            {
                UtilityScore.Consideration consideration = UtilityScore.Ratio(path.Cost, stack.MovementPoints);

                return MakePlan(consideration.Score, new WorldAiMoveStackCommand(stack.Id, movePosition.Value), memory);
            }

            return null;
        }

        private static WorldAiMemory RememberTarget(ActionContext context, WorldAiTarget target, ScoringType scoring)
        {
            WorldAiStackMemory stackMemory = context.Memory.GetStackMemory(context.Stack);

            return scoring switch
            {
                ScoringType.Default => context.Memory,
                ScoringType.Ordered => context.Memory,
                ScoringType.Committed => context.Memory.WithStackMemory(context.Stack.Id, stackMemory with { Target = target })
            };
        }

        private IReadOnlyList<WorldAiPathfinding.TargetPath> ScoreTargets(
            ActionContext context,
            WorldPathMap pathMap,
            IEnumerable<WorldAiTarget> targets,
            ScoringType scoring)
        {
            IReadOnlyList<WorldAiPathfinding.TargetPath> targetPaths = Context.Pathfinding.GetTargetPaths(context.State, pathMap, targets);
            WorldAiStackMemory stackMemory = context.Memory.GetStackMemory(context.Stack);

            return scoring switch
            {
                ScoringType.Default =>
                [
                    .. EnumerateScoredTargets(context, pathMap, targetPaths)
                        .OrderByDescending(entry => entry.Score)
                        .Select(entry => entry.TargetPath)
                ],
                ScoringType.Ordered =>
                [
                    .. targetPaths
                        .OrderBy(targetPath => targetPath.Path.Cost)
                        .Take(1)
                ],
                ScoringType.Committed =>
                [
                    .. EnumerateScoredTargets(context, pathMap, targetPaths)
                        .OrderByDescending(entry => entry.TargetPath.Target == stackMemory.Target)
                        .ThenByDescending(entry => entry.Score)
                        .Select(entry => entry.TargetPath)
                ]
            };
        }

        private IEnumerable<(WorldAiPathfinding.TargetPath TargetPath, int Score)> EnumerateScoredTargets(
            ActionContext context,
            WorldPathMap pathMap,
            IReadOnlyList<WorldAiPathfinding.TargetPath> targetPaths)
        {
            WorldPlayer player = context.State.Players[context.Stack.OwnerId];
            WorldAiAttitudeDefinition attitude = Context.Engine.Definitions.GetAiAttitude(player.AiAttitude);
            WorldAiTargetInterestVisitor interestVisitor = new(attitude);
            WorldAiTargetInterestVisitor.Context interestContext = new(context.State, context.Stack.OwnerId);
            WorldAiTargetPriorityVisitor priorityVisitor = new();
            WorldAiTargetPriorityVisitor.Context priorityContext = new(context.State);
            StackStats stackStats = Context.Engine.UnitStats.GetWorldStackStats(context.Stack);

            foreach (WorldAiPathfinding.TargetPath targetPath in targetPaths)
            {
                int interest = targetPath.Target.Accept(interestVisitor, interestContext);
                int priority = targetPath.Target.Accept(priorityVisitor, priorityContext);
                PathTraversalCalculator.TraversalInfo traversal = Context.Pathfinding.GetTraversalInfo(context.State, context.Stack, targetPath.Path);
                BattleInfo? battleInfo = GetBattleInfo(
                    Context, context.State, context.Stack, targetPath.Target, targetPath.Position, traversal.Encounter);

                List<UtilityScore.Consideration> considerations =
                [
                    Considerations.TargetInterest(interest),
                    Considerations.TargetPriority(priority),
                    Considerations.TargetMoveCost(targetPath.Path.Cost, stackStats.MaxMovementPoints * MoveTurnLimit),
                    Considerations.PowerPrefilter(battleInfo),
                    Considerations.TargetSurvival(Context.Estimator, context, battleInfo),
                    Considerations.VitalStackSurvival(Context, context, pathMap, targetPath),
                    Considerations.ScenarioObjectiveTarget(context, targetPath.Target),
                    Considerations.ScenarioBlockedTarget(context, targetPath.Target)
                ];

                int score = UtilityScore.Combine(considerations);

                if (score > 0)
                {
                    yield return (targetPath, score);
                }
            }
        }

        private bool CanPickUpBags(WorldState state, WorldStack stack)
        {
            WorldPlayer owner = state.Players[stack.OwnerId];

            if (WorldHelpers.IsRacePlayable(owner.Race))
            {
                (_, _, UnitDefinition leaderDefinition) = stack.GetLeader(Context.Engine.Definitions);

                if (leaderDefinition.Category is UnitCategory.Summon or UnitCategory.Illusion)
                {
                    return false;
                }
                else
                {
                    return leaderDefinition.LeaderCategory switch
                    {
                        UnitLeaderCategory.Fighter => true,
                        UnitLeaderCategory.Explorer => true,
                        UnitLeaderCategory.Mage => true,
                        UnitLeaderCategory.Thief => true,
                        UnitLeaderCategory.Rod => false,
                        null => false
                    };
                }
            }
            else
            {
                return true;
            }
        }
    }
}
