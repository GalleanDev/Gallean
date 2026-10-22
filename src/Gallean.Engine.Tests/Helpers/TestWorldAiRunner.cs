using Gallean.Tests.Common;

namespace Gallean.Engine.Tests;

internal sealed class TestWorldAiRunner(GameSession session, IEngineContext context)
{
    public sealed record Trace(WorldAiActionPlan Plan, BattleOutcome? Battle, WorldStackOrder? StackOrder);

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

    private BattleOutcome? ResolveBattle()
    {
        BattleSession? battle = session.Battle;

        if (battle is null)
        {
            return null;
        }

        BattleCommandResult result = battle.DispatchCommand(new BattleAutoResolveCommand());
        Assert.That(result.Error, Is.Null);

        return Assert.NotNull(battle.State.Outcome);
    }
}
