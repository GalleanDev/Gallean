using Gallean.Common;

namespace Gallean.Engine;

internal static partial class StackOrderCalculator
{
    private sealed class IsAssignableVisitor(WorldStack stack, WorldState state) : IWorldStackOrderVisitor<Empty, bool>
    {
        public bool Visit(WorldStackOrder.Normal order, Empty context)
        {
            return !IsNeutral(stack, state);
        }

        public bool Visit(WorldStackOrder.Stand order, Empty context)
        {
            return true;
        }

        public bool Visit(WorldStackOrder.Guard order, Empty context)
        {
            return true;
        }

        public bool Visit(WorldStackOrder.AttackStack order, Empty context)
        {
            if (order.TargetId == stack.Id)
            {
                return false;
            }

            if (state.Stacks.TryGetValue(order.TargetId, out WorldStack? target))
            {
                return target.SubraceId != stack.SubraceId || IsNeutralSubrace(stack, state);
            }
            else
            {
                return false;
            }
        }

        public bool Visit(WorldStackOrder.DefendStack order, Empty context)
        {
            if (IsNeutralSubrace(stack, state))
            {
                return false;
            }

            if (order.TargetId == stack.Id)
            {
                return false;
            }

            if (state.Stacks.TryGetValue(order.TargetId, out WorldStack? target))
            {
                return target.SubraceId == stack.SubraceId;
            }
            else
            {
                return false;
            }
        }

        public bool Visit(WorldStackOrder.SecureCity order, Empty context)
        {
            if (IsNeutralSubrace(stack, state))
            {
                return false;
            }

            if (state.Fortifications.TryGetValue(order.TargetId, out WorldFortification? fortification))
            {
                return fortification is WorldCity or WorldCapital;
            }
            else
            {
                return false;
            }
        }

        public bool Visit(WorldStackOrder.Roam order, Empty context)
        {
            return true;
        }

        public bool Visit(WorldStackOrder.Explore order, Empty context)
        {
            return true;
        }

        public bool Visit(WorldStackOrder.MoveToLocation order, Empty context)
        {
            return true;
        }

        public bool Visit(WorldStackOrder.DefendLocation order, Empty context)
        {
            return true;
        }

        public bool Visit(WorldStackOrder.Berserk order, Empty context)
        {
            return true;
        }

        public bool Visit(WorldStackOrder.Assist order, Empty context)
        {
            if (order.TargetId == stack.Id)
            {
                return false;
            }

            if (state.Stacks.TryGetValue(order.TargetId, out WorldStack? target))
            {
                return target.SubraceId == stack.SubraceId &&
                       target.Order is WorldStackOrder.Normal;
            }
            else
            {
                return false;
            }
        }

        public bool Visit(WorldStackOrder.Steal order, Empty context)
        {
            return true;
        }

        public bool Visit(WorldStackOrder.DefendCity order, Empty context)
        {
            if (state.Fortifications.TryGetValue(order.TargetId, out WorldFortification? fortification))
            {
                return fortification is WorldCity or WorldCapital;
            }
            else
            {
                return false;
            }
        }

        public bool Visit(WorldStackOrder.Heal order, Empty context)
        {
            return false;
        }

        public bool Visit(WorldStackOrder.Reinforce order, Empty context)
        {
            return false;
        }
    }
}
