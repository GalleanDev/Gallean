using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldAiOrderPlanner
{
    public readonly record struct OrderContext(WorldState State, WorldStack Stack);

    private const int HitpointsScale = 1000;
    private const int HurtThreshold = 800;
    private const int HealedThreshold = 900;

    private const int StrengthScale = 1000;
    private const int ReinforceThreshold = 800;

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.Normal order, OrderContext context)
    {
        return PlanHeal(context) ?? SelectBasicOrder(context);
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.Stand order, OrderContext context)
    {
        return order;
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.Guard order, OrderContext context)
    {
        return order;
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.AttackStack order, OrderContext context)
    {
        return order;
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.DefendStack order, OrderContext context)
    {
        return order;
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.SecureCity order, OrderContext context)
    {
        return order;
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.Roam order, OrderContext context)
    {
        return PlanHeal(context) ?? SelectBasicOrder(context);
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.Explore order, OrderContext context)
    {
        return SelectBasicOrder(context);
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.MoveToLocation order, OrderContext context)
    {
        return order;
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.DefendLocation order, OrderContext context)
    {
        return order;
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.Berserk order, OrderContext context)
    {
        return order;
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.Assist order, OrderContext context)
    {
        return order;
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.Steal order, OrderContext context)
    {
        return order;
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.DefendCity order, OrderContext context)
    {
        return order;
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.Heal order, OrderContext context)
    {
        if (GetHitpointsRatio(context.Stack) >= HealedThreshold)
        {
            return SelectBasicOrder(context);
        }

        return order;
    }

    WorldStackOrder IWorldStackOrderVisitor<OrderContext, WorldStackOrder>.Visit(WorldStackOrder.Reinforce order, OrderContext context)
    {
        return PlanHeal(context) ?? SelectBasicOrder(context);
    }

    private WorldStackOrder SelectBasicOrder(OrderContext context)
    {
        WorldStackOrder? order = null;

        order ??= PlanExplore(context);
        order ??= PlanReinforce(context);

        return order ?? WorldStackOrder.Normal.Instance;
    }

    private WorldStackOrder? PlanHeal(OrderContext context)
    {
        if (GetHitpointsRatio(context.Stack) < HurtThreshold)
        {
            return WorldStackOrder.Heal.Instance;
        }
        else
        {
            return null;
        }
    }

    private WorldStackOrder? PlanExplore(OrderContext context)
    {
        if (IsRodLeader(context.Stack) || IsThiefLeader(context.Stack))
        {
            return WorldStackOrder.Explore.Instance;
        }
        else
        {
            return null;
        }
    }

    private int GetHitpointsRatio(WorldStack stack)
    {
        int current = 0;
        int maximum = 0;

        foreach ((WorldUnit unit, _) in stack.Units.GetUnits())
        {
            current += unit.Hitpoints;
            maximum += Context.Engine.UnitStats.GetWorldUnitStats(unit).MaxHitpoints;
        }

        if (maximum > 0)
        {
            return current * HitpointsScale / maximum;
        }
        else
        {
            return HitpointsScale;
        }
    }

    private WorldStackOrder? PlanReinforce(OrderContext context)
    {
        WorldState state = context.State;
        WorldStack stack = context.Stack;
        int strength = GetStrengthRatio(stack.Units);

        if (strength >= ReinforceThreshold)
        {
            return null;
        }

        WorldPathMap? pathMap = Context.Pathfinding.GetPathMap(state, stack);

        if (pathMap is null)
        {
            return null;
        }

        WorldPlan plan = WorldPlan.For(state);

        IEnumerable<WorldAiTarget> targets = state.Fortifications.Values
            .Where(fortification => FortificationCalculator.GetOwner(fortification) == stack.OwnerId)
            .Select(fortification => new WorldAiTarget.Fortification(fortification.Id));

        IReadOnlyList<WorldAiPathfinding.TargetPath> targetPaths = Context.Pathfinding.GetTargetPaths(state, pathMap, targets);

        foreach (WorldAiPathfinding.TargetPath targetPath in targetPaths.OrderBy(targetPath => targetPath.Path.Cost))
        {
            WorldStack? fortificationStack = plan.GetStackAt(targetPath.Position);

            if (targetPath.Target is WorldAiTarget.Fortification fortificationTarget &&
                (fortificationStack is null || fortificationStack.Id == stack.Id))
            {
                WorldFortification fortification = state.Fortifications[fortificationTarget.FortificationId];

                StackAutoFitCalculator.Result autoFitResult = StackAutoFitCalculator.Calculate(
                    stack.Units,
                    fortification.Garrison,
                    stack.Inventory,
                    fortification.Inventory,
                    stack.Equipment,
                    GarrisonCalculator.GetTotalCapacity(fortification, Context.Engine.Globals),
                    Context.Engine.Definitions);

                if (GetStrengthRatio(autoFitResult.StackUnits) > strength)
                {
                    return new WorldStackOrder.Reinforce(fortification.Id);
                }
            }
        }

        return null;
    }

    private int GetStrengthRatio(UnitGrid<WorldUnit> units)
    {
        int totalCapacity = 0;
        int occupiedCapacity = 0;

        foreach ((WorldUnit unit, _) in units.GetUnits())
        {
            UnitDefinition definition = Context.Engine.Definitions.GetUnit(unit.DefinitionId);

            if (definition.Leadership is > 0)
            {
                int leadership = UnitStatsCalculator.GetLeadership(definition, unit.Modifiers, Context.Engine.Definitions);

                totalCapacity = Math.Min(leadership, GridPosition.Count);
            }

            if (unit.IsAlive)
            {
                occupiedCapacity += definition.IsLarge ? GridPosition.LineCount : 1;
            }
        }

        if (totalCapacity > 0)
        {
            return occupiedCapacity * StrengthScale / totalCapacity;
        }
        else
        {
            return StrengthScale;
        }
    }

    private bool IsRodLeader(WorldStack stack)
    {
        foreach ((WorldUnit unit, _) in stack.Units.GetUnits())
        {
            UnitDefinition definition = Context.Engine.Definitions.GetUnit(unit.DefinitionId);

            if (definition.Leadership is > 0)
            {
                return definition.LeaderCategory == UnitLeaderCategory.Rod;
            }
        }

        return false;
    }

    private bool IsThiefLeader(WorldStack stack)
    {
        return ThiefCalculator.IsThief(stack, Context.Engine.Definitions);
    }
}
