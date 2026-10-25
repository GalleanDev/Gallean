namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed record EnterCityAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
    {
        private const int MaxDistance = 250;
        private const int ExitStackDistance = 1;
        private const int ExitStackHitpointsPercent = 90;

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
            if (context.State.Fortifications.TryGetValue(order.TargetId, out WorldFortification? fortification))
            {
                return PlanExitStack(context, fortification);
            }
            else
            {
                return null;
            }
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Roam order, ActionContext context)
        {
            WorldPlan plan = WorldPlan.For(context.State);
            WorldFortification? fortification = plan.GetFortificationAt(context.Stack.Position);

            if (fortification is not (WorldCity or WorldCapital { CapturedById: null }))
            {
                return PlanEnterCity(context);
            }
            else
            {
                return null;
            }
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Explore order, ActionContext context)
        {
            return null;
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
            foreach (WorldFortification fortification in context.State.Fortifications.Values)
            {
                if (FortificationCalculator.GetOwner(fortification) == context.Stack.OwnerId)
                {
                    WorldAiActionPlan? plan = PlanExitStack(context, fortification);

                    if (plan is not null)
                    {
                        return plan;
                    }
                }
            }

            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Reinforce order, ActionContext context)
        {
            return null;
        }

        private WorldAiActionPlan? PlanEnterCity(ActionContext context)
        {
            WorldStack stack = context.Stack;

            (_, _, UnitDefinition leaderDefinition) = stack.GetLeader(Context.Engine.Definitions);

            if (leaderDefinition.IsWaterOnly)
            {
                return null;
            }

            if (leaderDefinition.Category is UnitCategory.Summon or UnitCategory.Illusion)
            {
                return null;
            }

            if (stack.MovementPoints <= 0)
            {
                return null;
            }

            WorldState state = context.State;
            WorldPathMap? pathMap = Context.Pathfinding.GetPathMap(state, stack);

            if (pathMap is null)
            {
                return null;
            }

            WorldPlan worldPlan = WorldPlan.For(state);

            IEnumerable<WorldAiTarget> cityTargets = state.Fortifications.Values
                .OfType<WorldCity>()
                .Where(city => city.SubraceId == stack.SubraceId)
                .Where(city => !FortificationCalculator.IsRioting(city))
                .Where(city => worldPlan.GetStackAt(city.Entrance) is null)
                .Select(city => new WorldAiTarget.Fortification(city.Id));

            IReadOnlyList<WorldAiPathfinding.TargetPath> paths = Context.Pathfinding.GetTargetPaths(state, pathMap, cityTargets);

            foreach (WorldAiPathfinding.TargetPath path in paths.OrderBy(path => stack.Position.GetDistance(path.Position)))
            {
                WorldAiActionPlan? plan = PlanMoveToward(context, pathMap, path.Position);

                if (plan is not null)
                {
                    return plan;
                }
            }

            return null;
        }

        private WorldAiActionPlan? PlanExitStack(ActionContext context, WorldFortification fortification)
        {
            WorldState state = context.State;

            if (context.Stack.MovementPoints <= 0)
            {
                return null;
            }

            if (context.Stack.Position.GetDistance(fortification.Entrance) != ExitStackDistance)
            {
                return null;
            }

            WorldPlan plan = WorldPlan.For(state);

            WorldStack? target = plan.GetStackAt(fortification.Entrance);

            if (target is null)
            {
                return null;
            }

            foreach ((WorldUnit unit, _) in target.Units.GetUnits())
            {
                UnitStats unitStats = Context.Engine.UnitStats.GetWorldUnitStats(unit);

                if (unit.Hitpoints * 100 <= unitStats.MaxHitpoints * ExitStackHitpointsPercent)
                {
                    return null;
                }
            }

            WorldExitStackCommand command = new(target.Id);
            WorldCommandResult commandResult = command.Execute(state, Context.Engine);

            if (commandResult.Failed)
            {
                return null;
            }

            return MakePlan(MaxScore, command, context.Memory);
        }

        private WorldAiActionPlan? PlanMoveToward(ActionContext context, WorldPathMap pathMap, WorldPosition targetPosition)
        {
            WorldStack stack = context.Stack;
            WorldPosition? movePosition = Context.Pathfinding.FindMoveTowardCell(context.State, stack, pathMap, targetPosition);

            if (movePosition is not null)
            {
                int score = UtilityScore.InverseRatio(stack.Position.GetDistance(targetPosition), MaxDistance).Score;

                return MakePlan(score, new WorldAiMoveStackCommand(stack.Id, movePosition.Value), context.Memory);
            }
            else
            {
                return null;
            }
        }
    }
}
