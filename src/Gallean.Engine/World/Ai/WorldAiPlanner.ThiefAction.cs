using static Gallean.Engine.WorldAiTargetEnumerator;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record ThiefAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
    {
        public override WorldAiActionPlan? Visit(WorldStackOrder.Normal order, ActionContext context)
        {
            return null;
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
            return PlanThiefAction(context);
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

        private WorldAiActionPlan? PlanThiefAction(ActionContext context)
        {
            WorldState state = context.State;
            WorldStack stack = context.Stack;

            if (!ThiefCalculator.IsThief(stack, Context.Engine.Definitions))
            {
                return null;
            }

            if (stack.MovementPoints <= 0)
            {
                return null;
            }

            WorldPathMap? pathMap = Context.Pathfinding.GetPathMap(state, stack);

            if (pathMap is null)
            {
                return null;
            }

            LordDefinition lord = Context.Engine.Definitions.GetLord(state.Players[stack.OwnerId].LordId);
            TargetVisitor.Context targetContext = new(state, stack.OwnerId, lord);

            IEnumerable<WorldAiTarget> targets =
                Enumerate(state, stack.OwnerId, TargetType.Enemy, area: null)
                    .Where(target => target.Accept(TargetVisitor.Instance, targetContext));

            IReadOnlyList<WorldAiPathfinding.TargetPath> paths = Context.Pathfinding.GetTargetPaths(state, pathMap, targets);

            foreach (WorldAiPathfinding.TargetPath path in paths.OrderBy(targetPath => targetPath.Path.Cost))
            {
                WorldAiActionPlan? plan;

                if (stack.Position.IsAdjacentTo(path.Position))
                {
                    plan = PlanAct(context, path.Position);
                }
                else
                {
                    plan = PlanMoveToward(context, pathMap, path.Position);
                }

                if (plan is not null)
                {
                    return plan;
                }
            }

            return null;
        }

        private WorldAiActionPlan? PlanAct(ActionContext context, WorldPosition targetPosition)
        {
            WorldThiefAction[] actions =
            [
                new WorldThiefRiotAction(targetPosition),
                new WorldThiefSpyAction(targetPosition)
            ];

            foreach (WorldThiefAction action in actions)
            {
                WorldThiefActionCommand command = new(context.Stack.Id, action);

                if (!command.Execute(context.State, Context.Engine).Failed)
                {
                    return MakePlan(MaxScore, command, context.Memory);
                }
            }

            return null;
        }

        private WorldAiActionPlan? PlanMoveToward(ActionContext context, WorldPathMap pathMap, WorldPosition targetPosition)
        {
            WorldStack stack = context.Stack;
            WorldPosition? movePositions = Context.Pathfinding.FindMoveTowardCell(context.State, stack, pathMap, targetPosition);

            if (movePositions is not null && pathMap.TryGetPath(movePositions.Value, WorldMovementLimit.Unlimited, out WorldPath? path))
            {
                int score = UtilityScore.Ratio(path.Cost, stack.MovementPoints).Score;

                return MakePlan(score, new WorldAiMoveStackCommand(stack.Id, movePositions.Value), context.Memory);
            }

            return null;
        }
    }
}
