using static Gallean.Engine.WorldAiTargetEnumerator;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record TakeBagAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
    {
        private const int RoamBagDistance = 3;

        public override WorldAiActionPlan? Visit(WorldStackOrder.Normal order, ActionContext context)
        {
            return PlanTakeBag(context, area: null);
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
            WorldPlan plan = WorldPlan.For(context.State);
            WorldFortification? fortification = plan.GetFortificationAt(context.Stack.Position);

            if (fortification is WorldCity or WorldCapital { CapturedById: null })
            {
                return null;
            }
            else
            {
                return PlanTakeBag(context, new TargetArea(context.Stack.Position, RoamBagDistance));
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
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Reinforce order, ActionContext context)
        {
            return null;
        }

        private WorldAiActionPlan? PlanTakeBag(ActionContext context, TargetArea? area)
        {
            WorldState state = context.State;
            WorldStack stack = context.Stack;

            if (stack.MovementPoints <= 0)
            {
                return null;
            }

            if (!CanPickUpBags(state, stack))
            {
                return null;
            }

            WorldPathMap? pathMap = Context.Pathfinding.GetPathMap(state, stack);

            if (pathMap is null)
            {
                return null;
            }

            WorldPlayer player = state.Players[stack.OwnerId];
            WorldAiAttitudeDefinition attitude = Context.Engine.Definitions.GetAiAttitude(player.AiAttitude);
            IEnumerable<WorldAiTarget> targets = Enumerate(state, stack.OwnerId, TargetType.Bag, area);
            (int Score, WorldPosition Position)? best = null;

            foreach (WorldAiPathfinding.TargetPath targetPath in Context.Pathfinding.GetTargetPaths(state, pathMap, targets))
            {
                PathTraversalCalculator.TraversalInfo traversal = Context.Pathfinding.GetTraversalInfo(state, stack, targetPath.Path);

                if (traversal.Encounter is PathTraversalCalculator.Encounter.Bag { CanBegin: true } bag &&
                    bag.Position == targetPath.Position)
                {
                    int distance = stack.Position.GetDistance(targetPath.Position);

                    int score = UtilityScore.Combine(
                    [
                        Considerations.BagInterest(attitude.BagInterest, distance),
                        Considerations.ScenarioObjectiveTarget(context, targetPath.Target),
                        Considerations.ScenarioBlockedTarget(context, targetPath.Target)
                    ]);

                    if (score > 0 && (best is null || score > best.Value.Score))
                    {
                        best = (score, targetPath.Position);
                    }
                }
            }

            if (best is null)
            {
                return null;
            }

            WorldAiMoveStackCommand command = new(stack.Id, best.Value.Position);

            return MakePlan(best.Value.Score, command, context.Memory);
        }
    }
}
