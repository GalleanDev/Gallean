using Gallean.Tests.Common;

namespace Gallean.Engine.Tests;

internal sealed class TestWorldAiRunner(GameSession session, IEngineContext context)
{
    public sealed record Trace(WorldAiActionPlan Plan, BattleTrace? Battle, WorldStackOrder? StackOrder);

    public sealed record BattleTrace(WorldStackId AttackerStackId, BattleOutcome Outcome);

    private readonly WorldAiController controller = new(context);
    private WorldAiMemory memory = WorldAiMemory.Empty;

    public IReadOnlyList<Trace> PlayTurn()
    {
        List<Trace> traces = [];

        while (true)
        {
            WorldState state = session.World.State;
            WorldAiActionPlan plan = controller.DecideAction(state, memory);
            WorldStackOrder? stackOrder = GetStackOrder(state, plan);

            memory = plan.Memory;

            WorldCommandResult result = session.World.DispatchCommand(plan.Command);
            Assert.That(result.Error, Is.Null);

            if (plan.Command is WorldEndTurnCommand)
            {
                break;
            }

            traces.Add(new Trace(plan, ResolveBattle(), stackOrder));
        }

        return traces;
    }

    private static WorldStackOrder? GetStackOrder(WorldState state, WorldAiActionPlan plan)
    {
        if (plan.Command is WorldAiMoveStackCommand command && state.Stacks.TryGetValue(command.StackId, out WorldStack? stack))
        {
            if (stack.Order is WorldStackOrder.Normal)
            {
                WorldAiStackMemory stackMemory = plan.Memory.GetStackMemory(stack);

                return stackMemory.Order;
            }
            else
            {
                return stack.Order;
            }
        }
        else
        {
            return null;
        }
    }

    private static WorldStackId? FindAttackerStackId(WorldState state, BattleState battle)
    {
        foreach (WorldStack stack in state.Stacks.Values)
        {
            if (HasStackUnit(battle.AttackerGrid, stack))
            {
                return stack.Id;
            }
        }

        return null;

        static bool HasStackUnit(UnitGrid<BattleUnit> grid, WorldStack stack)
        {
            foreach ((BattleUnit battleUnit, GridPosition _) in grid.GetUnits())
            {
                foreach ((WorldUnit worldUnit, GridPosition _) in stack.Units.GetUnits())
                {
                    if (battleUnit.WorldUnitId == worldUnit.Id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    private BattleTrace? ResolveBattle()
    {
        BattleSession? battle = session.Battle;

        if (battle is null)
        {
            return null;
        }

        WorldStackId attackerStackId = Assert.NotNull(FindAttackerStackId(session.World.State, battle.State));

        BattleCommandResult result = battle.DispatchCommand(new BattleAutoResolveCommand());
        Assert.That(result.Error, Is.Null);

        BattleOutcome outcome = Assert.NotNull(battle.State.Outcome);

        return new BattleTrace(attackerStackId, outcome);
    }
}
