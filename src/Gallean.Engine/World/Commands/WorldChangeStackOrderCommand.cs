using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldChangeStackOrderCommand(WorldStackId StackId, WorldCanonicalStackOrder Order) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.CurrentPlayer;

        if (!player.IsAi)
        {
            return WorldCommandResult.Fail(WorldCommandError.NotAiTurn);
        }

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (stack.Order.Equals(Order))
        {
            return WorldCommandResult.Success(Empty.Value, []);
        }

        return WorldCommandResult.Success(Empty.Value, [new WorldStackOrderChangedEvent(StackId, Order)]);
    }
}
