using Gallean.Common;

namespace Gallean.Engine;

internal static partial class StackOrderCalculator
{
    public static WorldCanonicalStackOrder CalculateCreatedOrder(WorldStack stack, WorldState state)
    {
        if (IsAssignable(stack.Order, stack, state))
        {
            return stack.Order;
        }
        else if (stack.Order is WorldStackOrder.AttackStack)
        {
            return WorldStackOrder.Berserk.Instance;
        }
        else if (IsNeutral(stack, state))
        {
            return WorldStackOrder.Roam.Instance;
        }
        else
        {
            return WorldStackOrder.Normal.Instance;
        }
    }

    public static WorldCanonicalStackOrder? CalculateChangedOrder(WorldCanonicalStackOrder order, WorldStack stack, WorldState state)
    {
        if (order is WorldStackOrder.Normal && IsNeutral(stack, state))
        {
            return WorldStackOrder.Stand.Instance;
        }
        else if ((order is WorldStackOrder.DefendStack or WorldStackOrder.SecureCity) && IsNeutralSubrace(stack, state))
        {
            return WorldStackOrder.Stand.Instance;
        }
        else if (IsAssignable(order, stack, state))
        {
            return order;
        }
        else
        {
            return null;
        }
    }

    private static bool IsAssignable(WorldCanonicalStackOrder order, WorldStack stack, WorldState state)
    {
        return order.Accept(new IsAssignableVisitor(stack, state), Empty.Value);
    }

    private static bool IsNeutral(WorldStack stack, WorldState state)
    {
        WorldPlayer owner = state.Players[stack.OwnerId];

        return owner.Race == WorldRace.Neutral;
    }

    private static bool IsNeutralSubrace(WorldStack stack, WorldState state)
    {
        WorldSubrace subrace = state.Subraces[stack.SubraceId];

        return subrace.Subrace == WorldSubraceType.Neutral;
    }
}
