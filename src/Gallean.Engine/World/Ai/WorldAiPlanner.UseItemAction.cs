namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed record UseItemAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
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
            return PlanAction(context);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Reinforce order, ActionContext context)
        {
            return PlanAction(context);
        }

        private WorldAiActionPlan? PlanAction(ActionContext context)
        {
            WorldStack stack = context.Stack;
            (int Score, WorldUseItemCommand Command)? best = null;

            foreach (WorldItem item in stack.Inventory.Items)
            {
                ItemDefinition itemDefinition = Context.Engine.Definitions.GetItem(item.DefinitionId);

                foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
                {
                    WorldUseItemCommand command = new(stack.Id, item.Id, new WorldItemTarget.Unit(unit.Id));
                    WorldCommandResult commandResult = command.Execute(context.State, Context.Engine);

                    if (!commandResult.Failed)
                    {
                        int? score = ScoreUsage(itemDefinition.Effect, unit);

                        if (score is not null)
                        {
                            if (best is null || score.Value > best.Value.Score)
                            {
                                best = (score.Value, command);
                            }
                        }
                    }
                }
            }

            if (best is null)
            {
                return null;
            }

            return MakePlan(best.Value.Score, best.Value.Command, context.Memory);
        }

        private int? ScoreUsage(ItemEffect? effect, WorldUnit unit)
        {
            switch (effect)
            {
                case ReviveItemEffect:
                    return MaxScore;
                case HealItemEffect:
                    UnitStats stats = Context.Engine.UnitStats.GetWorldUnitStats(unit);
                    return (stats.MaxHitpoints - unit.Hitpoints) * MaxScore / stats.MaxHitpoints;
                default:
                    return null;
            }
        }
    }
}
