using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed record RodAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
    {
        private readonly record struct RodTarget(WorldPosition Position, int Range, IWorldCommand<Empty> Command);

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

            List<RodTarget> targets = GetTargets(state, stack, pathMap);

            foreach (RodTarget target in targets)
            {
                WorldCommandResult commandResult = target.Command.Execute(state, Context.Engine);

                if (!commandResult.Failed)
                {
                    return MakePlan(MaxScore, target.Command, context.Memory);
                }
            }

            (WorldPosition Position, int Cost)? destination = null;

            foreach (RodTarget target in targets)
            {
                if (stack.Position.GetDistance(target.Position) <= target.Range)
                {
                    continue;
                }

                foreach (WorldPosition position in ScenarioLocationCalculator.EnumerateTiles(target.Position, target.Range, state.Map))
                {
                    if (position != target.Position &&
                        pathMap.TryGetPath(position, WorldMovementLimit.Unlimited, out WorldPath? path) &&
                        (destination is null || path.Cost < destination.Value.Cost))
                    {
                        PathTraversalCalculator.TraversalInfo traversal = Context.Pathfinding.GetTraversalInfo(state, stack, path);

                        if (traversal.Encounter is not PathTraversalCalculator.Encounter.Battle)
                        {
                            destination = (position, path.Cost);
                        }
                    }
                }
            }

            if (destination is null)
            {
                return null;
            }

            return PlanMoveToward(context, pathMap, destination.Value.Position);
        }

        private List<RodTarget> GetTargets(WorldState state, WorldStack stack, WorldPathMap pathMap)
        {
            List<RodTarget> targets = [];

            foreach (WorldRod rod in state.Rods.Values)
            {
                if (DiplomacyCalculator.IsHostile(state, stack.OwnerId, rod.OwnerId))
                {
                    WorldRemoveRodCommand command = new(stack.Id, rod.Position);

                    targets.Add(new RodTarget(rod.Position, Context.Engine.Globals.RodRemoveRange, command));
                }
            }

            WorldPlayer player = state.Players[stack.OwnerId];

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

                                targets.Add(new RodTarget(position, Context.Engine.Globals.RodPlantRange, command));
                            }
                        }
                    }
                }
            }

            return targets;
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

        private WorldAiActionPlan? PlanMoveToward(ActionContext context, WorldPathMap pathMap, WorldPosition targetPosition)
        {
            WorldStack stack = context.Stack;
            WorldPosition? movePosition = Context.Pathfinding.FindMoveTowardCell(context.State, stack, pathMap, targetPosition);

            if (movePosition is not null && pathMap.TryGetPath(movePosition.Value, WorldMovementLimit.Unlimited, out WorldPath? path))
            {
                UtilityScore.Consideration consideration = UtilityScore.Ratio(path.Cost, stack.MovementPoints);

                return MakePlan(consideration.Score, new WorldAiMoveStackCommand(stack.Id, movePosition.Value), context.Memory);
            }

            return null;
        }
    }
}
