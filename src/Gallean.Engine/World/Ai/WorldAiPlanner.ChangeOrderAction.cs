using Gallean.Common;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed record ChangeOrderAction(WorldAiContext Context, int Priority) : AbstractStackAction(Context, Priority)
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
            if (!context.State.Stacks.TryGetValue(order.TargetId, out WorldStack? target) || target.Concealed)
            {
                return PlanAction(context, WorldStackOrder.Berserk.Instance);
            }
            else
            {
                return null;
            }
        }

        public override WorldAiActionPlan? Visit(WorldStackOrder.DefendStack order, ActionContext context)
        {
            if (!context.State.Stacks.ContainsKey(order.TargetId))
            {
                WorldRace race = context.State.Players[context.Stack.OwnerId].Race;

                if (WorldHelpers.IsRacePlayable(race))
                {
                    return PlanAction(context, WorldStackOrder.Normal.Instance);
                }
                else
                {
                    return PlanAction(context, WorldStackOrder.Roam.Instance);
                }
            }
            else
            {
                return null;
            }
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
            if (context.Stack.Position.GetDistance(order.TargetPosition) <= order.Radius)
            {
                return PlanAction(context, WorldStackOrder.Stand.Instance);
            }
            else
            {
                return null;
            }
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

        private WorldAiActionPlan? PlanAction(ActionContext context, WorldCanonicalStackOrder order)
        {
            WorldChangeStackOrderCommand command = new(context.Stack.Id, order);

            WorldCommandResult commandResult = command.Execute(context.State, Context.Engine);
            if (commandResult.Failed || commandResult.Events.IsEmpty)
            {
                return null;
            }

            WorldAiMemory memory = context.Memory with
            {
                Stacks = context.Memory.Stacks.Remove(context.Stack.Id)
            };

            return MakePlan(MaxScore, command, memory);
        }
    }
}
