using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldUpgradeCityCommand(WorldFortificationId CityId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.CurrentPlayer;

        if (!WorldCommandHelpers.TryGetCurrentPlayerFortification(state, CityId, out WorldFortification? fortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.FortificationNotFound);
        }

        if (fortification is not WorldCity city)
        {
            return WorldCommandResult.Fail(WorldCommandError.FortificationNotFound);
        }

        if (FortificationCalculator.IsRioting(city))
        {
            return WorldCommandResult.Fail(WorldCommandError.CityRioting);
        }

        WorldCityDefinition definition = context.Definitions.GetCity(city.DefinitionId);
        WorldRaceDefinition race = context.Definitions.GetRace(city.DefinitionId.Race);
        WorldCityDefinitionId upgradedDefinitionId = city.DefinitionId with { Level = definition.Level + 1 };

        if (definition.Level >= context.Scenario.Definition.MaxCityLevel ||
            !race.Cities.Contains(upgradedDefinitionId))
        {
            return WorldCommandResult.Fail(WorldCommandError.CityFullyUpgraded);
        }

        if (city.GrowthTurn == state.TurnInfo.TurnNumber)
        {
            return WorldCommandResult.Fail(WorldCommandError.CityAlreadyGrownThisTurn);
        }

        WorldResourceSet growthCost = CityCalculator.GetGrowthCost(definition, player, context);

        if (!ResourceSetCalculator.CanAfford(player.Resources, growthCost))
        {
            return WorldCommandResult.Fail(WorldCommandError.InsufficientResources);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldCityUpgradedEvent(CityId, upgradedDefinitionId, growthCost)]);
    }
}
