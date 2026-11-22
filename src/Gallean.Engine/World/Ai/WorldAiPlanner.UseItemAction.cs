namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed record UseItemAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
    {
        public override WorldAiActionPlan? Visit(WorldStackOrder.Normal order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Stand order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Guard order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.AttackStack order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendStack order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.SecureCity order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Roam order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Explore order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.MoveToLocation order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendLocation order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Berserk order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Assist order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Steal order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendCity order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Heal order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionHeal, ItemCategory.PotionRevive, ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.Reinforce order, ActionContext context)
        {
            return PlanAction(context, [ItemCategory.PotionHeal, ItemCategory.PotionRevive, ItemCategory.PotionPermanentBoost, ItemCategory.PotionTemporaryBoost]);
        }

        private WorldAiActionPlan? PlanAction(ActionContext context, ReadOnlySpan<ItemCategory> categories)
        {
            WorldStack stack = context.Stack;
            (int Score, WorldUseItemCommand Command)? best = null;

            foreach (WorldItem item in stack.Inventory.Items)
            {
                ItemDefinition itemDefinition = Context.Engine.Definitions.GetItem(item.DefinitionId);

                if (categories.Contains(itemDefinition.Category))
                {
                    foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
                    {
                        WorldUnitGroup.Stack stackGroup = new(stack.Id);
                        WorldItemTarget.Unit unitTarget = new(stackGroup, unit.Id);
                        WorldUseItemCommand command = new(stackGroup, item.Id, unitTarget);
                        WorldCommandResult commandResult = command.Execute(context.State, Context.Engine);

                        if (!commandResult.Failed)
                        {
                            int? score = ScoreUsage(context, itemDefinition.Effect, unit);

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
            }

            if (best is null)
            {
                return null;
            }

            return MakePlan(best.Value.Score, best.Value.Command, context.Memory);
        }

        private int? ScoreUsage(ActionContext context, ItemEffect? effect, WorldUnit unit)
        {
            switch (effect)
            {
                case ReviveItemEffect:
                    return MaxScore;
                case HealItemEffect:
                    UnitStats stats = Context.Engine.UnitStats.GetWorldUnitStats(unit);
                    return (stats.MaxHitpoints - unit.Hitpoints) * MaxScore / stats.MaxHitpoints;
                case PermanentBoostItemEffect:
                    WorldPlayer owner = context.State.Players[context.Stack.OwnerId];
                    (WorldUnit leader, GridPosition _, UnitDefinition _) = context.Stack.GetLeader(Context.Engine.Definitions);

                    if (WorldHelpers.IsRacePlayable(owner.Race) && unit.Id == leader.Id)
                    {
                        return MaxScore;
                    }
                    else
                    {
                        return null;
                    }

                case TemporaryBoostItemEffect:
                    if (IsLeader(context, unit) && CanAttackPlayer(context))
                    {
                        return MaxScore;
                    }
                    else
                    {
                        return null;
                    }

                default:
                    return null;
            }
        }

        private bool IsLeader(ActionContext context, WorldUnit unit)
        {
            (WorldUnit leader, GridPosition _, UnitDefinition _) = context.Stack.GetLeader(Context.Engine.Definitions);

            return unit.Id == leader.Id;
        }

        private bool CanAttackPlayer(ActionContext context)
        {
            WorldState state = context.State;
            WorldStack stack = context.Stack;
            WorldAiStackMemory stackMemory = context.Memory.GetStackMemory(stack);

            if (stackMemory.Target is WorldAiTarget.Stack target &&
                state.Stacks.TryGetValue(target.StackId, out WorldStack? targetStack))
            {
                WorldPlayer owner = state.Players[stack.OwnerId];
                WorldPlayer targetOwner = state.Players[targetStack.OwnerId];
                WorldAiMoveStackCommand command = new(stack.Id, targetStack.Position);
                WorldCommandResult commandResult = command.Execute(state, Context.Engine);

                return WorldHelpers.IsRacePlayable(owner.Race) &&
                       WorldHelpers.IsRacePlayable(targetOwner.Race) &&
                       !ThiefCalculator.IsThief(stack, Context.Engine.Definitions) &&
                       !commandResult.Failed;
            }
            else
            {
                return false;
            }
        }
    }
}
