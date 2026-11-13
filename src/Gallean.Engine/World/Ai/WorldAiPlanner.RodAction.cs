using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record RodAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
    {
        private readonly record struct Target(WorldPosition Position, int Range, int Interest, IWorldCommand<Empty> Command);

        private readonly record struct Candidate(Target Target, WorldPosition Position, int Cost, int Score);

        private const int MoveTurnLimit = 10;

        public override WorldAiActionPlan? Visit(WorldStackOrder.Normal order, ActionContext context)
        {
            return PlanRod(context);
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
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendStack order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.SecureCity order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Roam order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Explore order, ActionContext context)
        {
            return PlanRod(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.MoveToLocation order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendLocation order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Berserk order, ActionContext context)
        {
            return null;
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
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Reinforce order, ActionContext context)
        {
            return null;
        }

        private WorldAiActionPlan? PlanRod(ActionContext context)
        {
            WorldState state = context.State;
            WorldStack stack = context.Stack;
            (WorldUnit leader, _, _) = stack.GetLeader(Context.Engine.Definitions);
            LeaderAbility leaderAbilities = Context.Engine.UnitStats.GetWorldUnitAbilities(leader);

            if (!leaderAbilities.HasFlag(LeaderAbility.Rod) || stack.MovementPoints <= 0)
            {
                return null;
            }

            WorldPathMap? pathMap = Context.Pathfinding.GetPathMap(state, stack);

            if (pathMap is null)
            {
                return null;
            }

            StackStats stackStats = Context.Engine.UnitStats.GetWorldStackStats(stack);
            Candidate? best = null;

            foreach (Target target in EnumerateTargets(state, stack, pathMap))
            {
                Candidate? candidate = FindBestCandidate(state, stack, pathMap, stackStats, target);

                if (candidate is not null && (best is null || candidate.Value.Score > best.Value.Score))
                {
                    best = candidate;
                }
            }

            if (best is null)
            {
                return null;
            }

            if (best.Value.Position == stack.Position)
            {
                return MakePlan(best.Value.Score, best.Value.Target.Command, context.Memory);
            }
            else
            {
                return PlanMoveToward(context, pathMap, best.Value);
            }
        }

        private Candidate? FindBestCandidate(WorldState state, WorldStack stack, WorldPathMap pathMap, StackStats stackStats, Target target)
        {
            if (stack.Position.GetDistance(target.Position) <= target.Range)
            {
                WorldCommandResult commandResult = target.Command.Execute(state, Context.Engine);

                if (!commandResult.Failed)
                {
                    int score = Score(target, 0, stackStats);

                    if (score > 0)
                    {
                        return new Candidate(target, stack.Position, 0, score);
                    }
                }

                return null;
            }

            Candidate? best = null;

            foreach (WorldPosition position in ScenarioLocationCalculator.EnumerateTiles(target.Position, target.Range, state.Map))
            {
                if (position != target.Position && pathMap.TryGetPath(position, WorldMovementLimit.Unlimited, out WorldPath? path))
                {
                    int score = Score(target, path.Cost, stackStats);

                    if (score > 0 && (best is null || score > best.Value.Score))
                    {
                        PathTraversalCalculator.TraversalInfo traversal = Context.Pathfinding.GetTraversalInfo(state, stack, path);

                        if (traversal.Encounter is not PathTraversalCalculator.Encounter.Battle)
                        {
                            best = new Candidate(target, position, path.Cost, score);
                        }
                    }
                }
            }

            return best;
        }

        private static int Score(Target target, int cost, StackStats stackStats)
        {
            return UtilityScore.Combine(
            [
                Considerations.TargetInterest(target.Interest),
                Considerations.TargetMoveCost(cost, stackStats.MaxMovementPoints * MoveTurnLimit)
            ]);
        }

        private IEnumerable<Target> EnumerateTargets(WorldState state, WorldStack stack, WorldPathMap pathMap)
        {
            WorldPlayer player = state.Players[stack.OwnerId];
            WorldAiAttitudeDefinition attitude = Context.Engine.Definitions.GetAiAttitude(player.AiAttitude);

            foreach (WorldRod rod in state.Rods.Values)
            {
                if (DiplomacyCalculator.IsHostile(state, stack.OwnerId, rod.OwnerId))
                {
                    WorldRemoveRodCommand command = new(stack.Id, rod.Position);

                    yield return new Target(rod.Position, Context.Engine.Globals.RodRemoveRange, attitude.RemoveRodInterest, command);
                }
            }

            if (ResourceSetCalculator.CanAfford(player.Resources, Context.Engine.Globals.RodCost))
            {
                WorldPlan plan = WorldPlan.For(state);

                foreach (WorldResourceNode node in state.ResourceNodes.Values)
                {
                    if (plan.GetLandOwner(node.Position) == stack.OwnerId)
                    {
                        continue;
                    }

                    if (!HasAdjacentRod(state, plan, node))
                    {
                        foreach (WorldPosition position in ScenarioLocationCalculator.EnumerateTiles(node.Position, 1, state.Map))
                        {
                            if (pathMap.TryGetPath(position, WorldMovementLimit.Unlimited, out WorldPath? _))
                            {
                                WorldCreateRodCommand command = new(stack.Id, position);

                                yield return new Target(position, Context.Engine.Globals.RodPlantRange, attitude.CreateRodInterest, command);
                            }
                        }
                    }
                }
            }
        }

        private static bool HasAdjacentRod(WorldState state, WorldPlan plan, WorldResourceNode node)
        {
            foreach (WorldPosition position in ScenarioLocationCalculator.EnumerateTiles(node.Position, 1, state.Map))
            {
                if (plan.GetRodAt(position) is not null)
                {
                    return true;
                }
            }

            return false;
        }

        private WorldAiActionPlan? PlanMoveToward(ActionContext context, WorldPathMap pathMap, Candidate candidate)
        {
            WorldStack stack = context.Stack;

            WorldPosition? movePosition = Context.Pathfinding.FindMoveTowardCell(context.State, stack, pathMap, candidate.Position);

            if (movePosition is not null)
            {
                WorldAiMoveStackCommand command = new(stack.Id, movePosition.Value);

                return MakePlan(candidate.Score, command, context.Memory);
            }

            return null;
        }
    }
}
