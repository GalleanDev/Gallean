using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldConstructBuildingCommand(
    WorldPlayerId PlayerId,
    WorldPlayerBuildingDefinitionId BuildingId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayerId currentPlayer = state.TurnInfo.CurrentPlayerId;

        if (PlayerId != currentPlayer)
        {
            return WorldCommandResult.Fail(WorldCommandError.NotPlayerTurn);
        }

        WorldPlayer player = state.CurrentPlayer;

        PlayerBuildingsCalculator.BuildingInfo info = PlayerBuildingsCalculator.GetInfo(player.Buildings, BuildingId, context.Definitions);

        WorldCommandError? statusError = info.Status switch
        {
            WorldPlayerBuildingStatus.Constructible => null,
            WorldPlayerBuildingStatus.Acquired => WorldCommandError.BuildingAlreadyExists,
            WorldPlayerBuildingStatus.ScenarioLocked => WorldCommandError.BuildingNotFound,
            WorldPlayerBuildingStatus.BranchLocked => WorldCommandError.BuildingBranchLocked,
            WorldPlayerBuildingStatus.MissingPrerequisite => WorldCommandError.BuildingMissingPrerequisite
        };

        if (statusError is not null)
        {
            return WorldCommandResult.Fail(statusError.Value);
        }

        WorldPlayerBuildingDefinition building = context.Definitions.GetPlayerBuilding(BuildingId);

        if (!ResourceSetCalculator.CanAfford(player.Resources, building.Cost))
        {
            return WorldCommandResult.Fail(WorldCommandError.InsufficientResources);
        }

        if (player.TurnActions.BuildingConstructed)
        {
            return WorldCommandResult.Fail(WorldCommandError.BuildingAlreadyConstructedThisTurn);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldBuildingConstructedEvent(PlayerId, BuildingId)]);
    }
}
