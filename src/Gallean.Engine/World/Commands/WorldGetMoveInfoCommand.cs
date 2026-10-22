using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetMoveInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetMoveInfoCommand(WorldStackId StackId) : IWorldCommand<WorldGetMoveInfoCommand.Result>
{
    public sealed record Result(ImmutableArray<WorldPosition> ReachablePositions)
    {
        public bool Equals(Result? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return ReachablePositions.SequenceEqual(other.ReachablePositions);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(ReachablePositions);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return CommandResult.Fail(WorldCommandError.StackNotFound);
        }

        WorldPlan plan = WorldPlan.For(state);
        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, context.Definitions);
        WorldPlayer player = state.Players[stack.OwnerId];
        WorldPathMap? pathMap = plan.GetPathMap(stack.Position, player.FogOfWar, profile);
        ImmutableArray<WorldPosition>.Builder positions = ImmutableArray.CreateBuilder<WorldPosition>();

        if (pathMap is not null)
        {
            ImmutableArray<WorldPosition> candidates = pathMap.GetReachablePositions(stack.MovementPoints);

            foreach (WorldPosition position in candidates)
            {
                if (IsReachable(state, context, position))
                {
                    positions.Add(position);
                }
            }
        }

        return CommandResult.Success(new Result(positions.ToImmutable()), []);
    }

    private bool IsReachable(WorldState state, IEngineContext context, WorldPosition position)
    {
        WorldCommandResult moveResult = new WorldMoveStackCommand(StackId, position).Execute(state, context);
        if (moveResult.Error is null)
        {
            return true;
        }

        WorldGetPathCommand pathCommand = new(StackId, position);
        ICommandResult<WorldCommandError, WorldEvent, WorldGetPathCommand.Result> pathResult = pathCommand.Execute(state, context);
        if (pathResult.Data is not null)
        {
            return pathResult.Data.Encounter == new WorldPathEncounter.Thief(position);
        }

        return false;
    }
}
