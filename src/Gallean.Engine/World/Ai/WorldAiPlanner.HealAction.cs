namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed record HealAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
    {
        public override WorldAiActionPlan? Visit(WorldStackOrder.Normal order, ActionContext context)
        {
            return PlanAction(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Stand order, ActionContext context)
        {
            return PlanAction(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Guard order, ActionContext context)
        {
            return PlanAction(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.AttackStack order, ActionContext context)
        {
            return PlanAction(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendStack order, ActionContext context)
        {
            return PlanAction(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.SecureCity order, ActionContext context)
        {
            return PlanAction(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Roam order, ActionContext context)
        {
            return PlanAction(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Explore order, ActionContext context)
        {
            return null;
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.MoveToLocation order, ActionContext context)
        {
            return PlanAction(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendLocation order, ActionContext context)
        {
            return PlanAction(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Berserk order, ActionContext context)
        {
            return PlanAction(context);
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
            return PlanAction(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Reinforce order, ActionContext context)
        {
            return PlanAction(context);
        }

        private WorldAiActionPlan? PlanAction(ActionContext context)
        {
            WorldStack stack = context.Stack;
            (int Score, WorldHealUnitCommand Command)? best = null;

            foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
            {
                WorldHealUnitCommand command = new(new WorldUnitGroup.Stack(stack.Id), unit.Id);
                WorldCommandResult commandResult = command.Execute(context.State, Context.Engine);

                if (!commandResult.Failed)
                {
                    UnitStats stats = Context.Engine.UnitStats.GetWorldUnitStats(unit);
                    int score = (stats.MaxHitpoints - unit.Hitpoints) * MaxScore / stats.MaxHitpoints;

                    if (best is null || score > best.Value.Score)
                    {
                        best = (score, command);
                    }
                }
            }

            if (best is null)
            {
                return null;
            }

            return MakePlan(best.Value.Score, best.Value.Command, context.Memory);
        }
    }
}
