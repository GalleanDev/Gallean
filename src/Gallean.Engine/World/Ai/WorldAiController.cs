using Gallean.Common;

namespace Gallean.Engine;

internal sealed class WorldAiController(IEngineContext engine)
{
    private const int MaxActionsPerTurn = 100;

    public WorldAiActionPlan DecideAction(WorldState state, WorldAiMemory memory, IWorldAiTrace? trace = null)
    {
        memory = memory.ReconcileWith(state);

        if (memory.Turn.ActionsChosen < MaxActionsPerTurn)
        {
            WorldPlan plan = WorldPlan.For(state);
            WorldAiPlanner planner = new(new WorldAiContext(engine, trace ?? NullWorldAiTrace.Instance));
            WorldPlayerId playerId = state.TurnInfo.CurrentPlayerId;
            WorldPlayer player = state.Players[playerId];

            if (WorldHelpers.IsRacePlayable(player.Race) && !plan.IsPlayerDefeated(playerId))
            {
                memory = DecideStackOrders(planner, state, playerId, memory);

                WorldAiActionPlan? economyPlan = planner.DecideEconomyAction(state, playerId, memory);

                if (economyPlan is not null)
                {
                    return FinalizePlan(economyPlan.Value);
                }
            }

            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (stack.OwnerId != playerId)
                {
                    continue;
                }

                WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

                if (fortification is not null && FortificationCalculator.IsRioting(fortification))
                {
                    continue;
                }

                if (!memory.Turn.IsStackExhausted(stack.Id))
                {
                    WorldAiActionPlan? stackPlan = planner.DecideStackAction(state, stack.Id, memory);
                    if (stackPlan is not null)
                    {
                        return FinalizePlan(stackPlan.Value);
                    }

                    memory = memory with { Turn = memory.Turn.WithStackExhausted(stack.Id) };
                }
            }
        }

        return WorldAiActionPlan.EndTurn(memory);
    }

    private static WorldAiMemory DecideStackOrders(WorldAiPlanner planner, WorldState state, WorldPlayerId playerId, WorldAiMemory memory)
    {
        foreach (WorldStack stack in state.Stacks.Values)
        {
            if (stack.OwnerId != playerId)
            {
                continue;
            }

            memory = planner.DecideStackOrder(state, stack, memory);
        }

        return memory;
    }

    private static WorldAiActionPlan FinalizePlan(WorldAiActionPlan plan)
    {
        return plan with
        {
            Memory = plan.Memory with { Turn = plan.Memory.Turn.WithAction(plan.Command) }
        };
    }
}
