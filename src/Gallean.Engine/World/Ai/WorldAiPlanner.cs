using System.Collections.Immutable;

namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner(WorldAiContext context)
{
    private readonly WorldAiOrderPlanner orderPlanner = new(context);
    private readonly IReadOnlyList<AbstractStackAction> stackActions = CreateStackActions(context);
    private readonly IReadOnlyList<AbstractEconomyAction> economyActions = CreateEconomyActions(context);

    public WorldAiActionPlan? DecideStackAction(WorldState state, WorldStackId stackId, WorldAiMemory memory)
    {
        WorldStack stack = state.Stacks[stackId];

        WorldStackOrder stackOrder;
        if (stack.Order is WorldStackOrder.Normal)
        {
            stackOrder = memory.GetStackMemory(stack).Order;
        }
        else
        {
            stackOrder = stack.Order;
        }

        ImmutableHashSet<WorldAiTarget> objectiveTargets;
        if (state.TurnInfo.TurnNumber >= GetObjectiveStartTurn(state.Difficulty))
        {
            objectiveTargets = context.Scenario.GetObjectiveTargets(state, stack.OwnerId);
        }
        else
        {
            objectiveTargets = [];
        }

        ImmutableHashSet<WorldAiTarget> blockedTargets = context.Scenario.GetBlockedTargets(state);

        bool isStackVital = context.Scenario.IsStackVital(state, stack);

        (AbstractStackAction Action, WorldAiActionPlan Plan)? best = null;

        foreach (AbstractStackAction action in stackActions)
        {
            WorldAiActionPlan? plan = action.Plan(state, stack, stackOrder, memory, objectiveTargets, blockedTargets, isStackVital);

            context.Trace.EvaluateStackAction(stack, action, plan);

            if (plan is not null && (best is null || plan.Value.Score > best.Value.Plan.Score))
            {
                best = (action, plan.Value);
            }
        }

        if (best is not null && CanExecutePlan(state, best.Value.Plan))
        {
            context.Trace.SelectStackAction(stack, best.Value.Action, best.Value.Plan);

            return best.Value.Plan;
        }

        return null;
    }

    public WorldAiMemory DecideStackOrder(WorldState state, WorldStack stack, WorldAiMemory memory)
    {
        if (stack.Order is WorldStackOrder.Normal)
        {
            WorldAiStackMemory stackMemory = memory.GetStackMemory(stack);
            WorldStackOrder stackOrder = orderPlanner.Decide(state, stack, stackMemory.Order);

            return memory.WithStackMemory(stack.Id, stackMemory with { Order = stackOrder });
        }
        else
        {
            return memory;
        }
    }

    public WorldAiActionPlan? DecideEconomyAction(WorldState state, WorldPlayerId playerId, WorldAiMemory memory)
    {
        (AbstractEconomyAction Action, WorldAiActionPlan Plan)? best = null;

        foreach (AbstractEconomyAction action in economyActions)
        {
            WorldAiActionPlan? plan = action.Plan(state, playerId, memory);

            context.Trace.EvaluateEconomyAction(playerId, action, plan);

            if (plan is not null && (best is null || plan.Value.Score > best.Value.Plan.Score))
            {
                best = (action, plan.Value);
            }
        }

        if (best is not null && CanExecutePlan(state, best.Value.Plan))
        {
            context.Trace.SelectEconomyAction(playerId, best.Value.Action, best.Value.Plan);

            return best.Value.Plan;
        }

        return null;
    }

    private bool CanExecutePlan(WorldState state, WorldAiActionPlan plan)
    {
        WorldCommandResult result = plan.Command.Execute(state, context.Engine);

        return !result.Failed;
    }

    private int GetObjectiveStartTurn(WorldDifficulty difficulty)
    {
        return difficulty switch
        {
            WorldDifficulty.Easy => context.Engine.Globals.AiObjectiveTurnEasy,
            WorldDifficulty.Average => context.Engine.Globals.AiObjectiveTurnAverage,
            WorldDifficulty.Hard => context.Engine.Globals.AiObjectiveTurnHard,
            WorldDifficulty.VeryHard => context.Engine.Globals.AiObjectiveTurnVeryHard
        };
    }

    private static IReadOnlyList<AbstractStackAction> CreateStackActions(WorldAiContext context)
    {
        return
        [
            new UseItemAction(context, Priority: 6),
            new ResurrectAction(context, Priority: 5),
            new HealAction(context, Priority: 4),
            new AutoFitAction(context, Priority: 3),
            new AttackAction(context, Priority: 2),
            new ThiefAction(context, Priority: 2),
            new EnterCityAction(context, Priority: 2),
            new MoveAction(context, Priority: 1),
            new ChangeOrderAction(context, Priority: 0)
        ];
    }

    private static IReadOnlyList<AbstractEconomyAction> CreateEconomyActions(WorldAiContext context)
    {
        return
        [
            new CastSpellAction(context, Priority: 4),
            new ResearchSpellAction(context, Priority: 3),
            new PurchaseLeaderAction(context, Priority: 2),
            new ConstructBuildingAction(context, Priority: 1),
            new PurchaseUnitAction(context, Priority: 0)
        ];
    }
}
