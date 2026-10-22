using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldRemoveRodCommand(
    WorldStackId StackId,
    WorldPosition TargetPosition) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayerId currentPlayer = state.TurnInfo.CurrentPlayerId;

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        (WorldUnit leader, _, _) = stack.GetLeader(context.Definitions);
        LeaderAbility leaderAbilities = context.UnitStats.GetWorldUnitAbilities(leader);

        if (!leaderAbilities.HasFlag(LeaderAbility.Rod))
        {
            return WorldCommandResult.Fail(WorldCommandError.RodLeaderRequired);
        }

        if (stack.MovementPoints <= 0)
        {
            return WorldCommandResult.Fail(WorldCommandError.InsufficientMovement);
        }

        WorldPlan plan = WorldPlan.For(state);

        if (plan.GetFortificationAt(stack.Position) is WorldCity or WorldCapital)
        {
            return WorldCommandResult.Fail(WorldCommandError.RodInvalidLocation);
        }

        if (!state.Map.IsValidPosition(TargetPosition))
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetOutOfBounds);
        }

        if (stack.Position.GetDistance(TargetPosition) > context.Globals.RodRemoveRange)
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetTooFar);
        }

        if (plan.GetRodAt(TargetPosition) is not { } rod)
        {
            return WorldCommandResult.Fail(WorldCommandError.RodNotFound);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldRodDestroyedEvent(rod.Id, currentPlayer, rod.OwnerId, rod.Position), new WorldStackMovementConsumedEvent(StackId, 0)]);
    }
}
