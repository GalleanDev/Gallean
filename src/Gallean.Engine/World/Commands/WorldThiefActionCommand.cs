using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldThiefActionCommand(
    WorldStackId StackId,
    WorldThiefAction Action) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayerId currentPlayer = state.TurnInfo.CurrentPlayerId;

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!ThiefCalculator.IsThief(stack, context.Definitions))
        {
            return WorldCommandResult.Fail(WorldCommandError.ThiefLeaderRequired);
        }

        if (stack.MovementPoints <= 0)
        {
            return WorldCommandResult.Fail(WorldCommandError.InsufficientMovement);
        }

        IWorldThiefActionResolver resolver = WorldThiefActionResolverFactory.Create(Action);

        return resolver.Resolve(state, context, stack, currentPlayer);
    }
}
