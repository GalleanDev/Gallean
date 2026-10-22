using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetCityInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetCityInfoCommand(WorldFortificationId FortificationId) : IWorldCommand<WorldGetCityInfoCommand.Result>
{
    public sealed record Growth(WorldResourceSet Cost, WorldCommandError? Error);

    public sealed record Result(Growth? Growth, WorldCommandError? HealError);

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.CurrentPlayer;

        if (!state.Fortifications.ContainsKey(FortificationId))
        {
            return CommandResult.Fail(WorldCommandError.FortificationNotFound);
        }

        WorldUnitGroup group = new WorldUnitGroup.Fortification(FortificationId);
        WorldCommandError? healError;

        if (!WorldUnitGroupCalculator.TryGetCity(state, group, out _, out WorldCommandError? cityError))
        {
            healError = cityError;
        }
        else if (!PlayerBuildingsCalculator.IsAcquired(player.Buildings, WorldPlayerBuildingCategory.Heal, context.Definitions))
        {
            healError = WorldCommandError.HealBuildingRequired;
        }
        else
        {
            healError = null;
        }

        if (WorldCommandHelpers.TryGetCurrentPlayerFortification(state, FortificationId, out WorldFortification? fortification) &&
            fortification is WorldCity city)
        {
            WorldCityDefinition definition = context.Definitions.GetCity(city.DefinitionId);
            WorldResourceSet growthCost = CityCalculator.GetGrowthCost(definition, player, context);
            WorldCommandResult upgradeResult = new WorldUpgradeCityCommand(FortificationId).Execute(state, context);

            return CommandResult.Success(new Result(new Growth(growthCost, upgradeResult.Error), healError), []);
        }
        else
        {
            return CommandResult.Success(new Result(Growth: null, healError), []);
        }
    }
}
