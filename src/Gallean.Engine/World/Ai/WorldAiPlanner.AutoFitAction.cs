namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed record AutoFitAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
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
            return PlanAction(context);
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
            WorldPlayer owner = context.State.Players[context.Stack.OwnerId];

            if (!WorldHelpers.IsRacePlayable(owner.Race))
            {
                return null;
            }

            WorldAutoFitStackCommand command = new(context.Stack.Id);

            WorldCommandResult commandResult = command.Execute(context.State, Context.Engine);
            if (commandResult.Failed || commandResult.Events.IsEmpty)
            {
                return null;
            }

            return MakePlan(MaxScore, command, context.Memory);
        }
    }
}
