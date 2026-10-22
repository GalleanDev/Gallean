using System.Collections.Immutable;
using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetBuildingsInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetBuildingsInfoCommand(WorldPlayerId PlayerId) : IWorldCommand<WorldGetBuildingsInfoCommand.Result>
{
    public sealed record Building(
        WorldPlayerBuildingDefinitionId BuildingId,
        WorldCommandError? Error,
        ImmutableArray<WorldPlayerBuildingDefinitionId> LockedIds)
    {
        public bool Equals(Building? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return BuildingId == other.BuildingId &&
                   Error == other.Error &&
                   LockedIds.SequenceEqual(other.LockedIds);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(BuildingId);
            hash.Add(Error);
            hash.AddSequence(LockedIds);

            return hash.ToHashCode();
        }
    }

    public sealed record Result(ImmutableArray<Building> Available)
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

            return Available.SequenceEqual(other.Available);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Available);

            return hash.ToHashCode();
        }
    }

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        if (PlayerId != state.TurnInfo.CurrentPlayerId)
        {
            return CommandResult.Fail(WorldCommandError.NotPlayerTurn);
        }

        WorldPlayer player = state.CurrentPlayer;

        ImmutableArray<Building>.Builder available = ImmutableArray.CreateBuilder<Building>();

        foreach (WorldPlayerBuildingDefinitionId buildingId in player.Buildings.Available)
        {
            WorldCommandResult constructResult = new WorldConstructBuildingCommand(PlayerId, buildingId).Execute(state, context);
            PlayerBuildingsCalculator.BuildingInfo info = PlayerBuildingsCalculator.GetInfo(player.Buildings, buildingId, context.Definitions);

            available.Add(new Building(buildingId, constructResult.Error, info.LockedIds));
        }

        return CommandResult.Success(new Result(available.ToImmutable()), []);
    }
}
