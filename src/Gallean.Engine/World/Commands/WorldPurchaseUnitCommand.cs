using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldPurchaseUnitCommand(
    WorldFortificationId FortificationId,
    UnitDefinitionId UnitDefinitionId,
    GridPosition Position) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.CurrentPlayer;

        if (!WorldCommandHelpers.TryGetCurrentPlayerFortification(state, FortificationId, out WorldFortification? fortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.FortificationNotFound);
        }

        if (fortification is not (WorldCity or WorldCapital))
        {
            return WorldCommandResult.Fail(WorldCommandError.FortificationNotFound);
        }

        if (FortificationCalculator.IsRioting(fortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.CityRioting);
        }

        WorldRaceDefinition raceDefinition = context.Definitions.GetRace(player.Race);

        if (!raceDefinition.Units.Contains(UnitDefinitionId))
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitNotAvailable);
        }

        UnitDefinition unitDefinition = context.Definitions.GetUnit(UnitDefinitionId);

        if (unitDefinition.RequiredBuildingId is not null &&
            !player.Buildings.Acquired.Contains(unitDefinition.RequiredBuildingId.Value))
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitMissingPrerequisite);
        }

        if (!UnitGrid<WorldUnit>.IsValidPosition(Position))
        {
            return WorldCommandResult.Fail(WorldCommandError.InvalidUnitPosition);
        }

        int capacity = GarrisonCalculator.GetTotalCapacity(fortification, context.Globals);
        WorldGarrisonBuilder builder = new(context.Definitions, fortification.Garrison, capacity);

        foreach ((WorldUnit unit, GridPosition position) in fortification.Garrison.GetUnits())
        {
            builder.PlaceUnit(unit, position);
        }

        string? name = LeaderNameCalculator.TryGetName(unitDefinition, context.Definitions, context.Random);
        WorldUnit newUnit = new(
            WorldCommandHelpers.GenerateUnitIds(state).First(),
            UnitDefinitionId,
            unitDefinition.MaxHitpoints,
            0,
            unitDefinition.Level,
            [],
            name,
            IsTypeLocked: false);

        if (!builder.TryPlaceUnit(newUnit, Position))
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetOccupied);
        }

        if (!builder.TryBuild(out _, out WorldCommandError? error))
        {
            return WorldCommandResult.Fail(error.Value);
        }

        if (!ResourceSetCalculator.CanAfford(player.Resources, unitDefinition.PurchaseCost))
        {
            return WorldCommandResult.Fail(WorldCommandError.InsufficientResources);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [
                new WorldUnitPurchasedEvent(
                    FortificationId,
                    newUnit,
                    Position,
                    unitDefinition.PurchaseCost)
            ]);
    }
}
