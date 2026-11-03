using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetObjectiveInfoCommand.Result>;

namespace Gallean.Engine;

public sealed record WorldGetObjectiveInfoCommand : IWorldCommand<WorldGetObjectiveInfoCommand.Result>
{
    public sealed record Result(LandInfo? Land);

    public sealed record LandInfo(int Percentage, int RequiredPercentage);

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.Players[state.TurnInfo.CurrentPlayerId];
        LandInfo? landInfo;

        int? requiredPercentage = ObjectiveCalculator.GetRequiredLandPercentage(context.Scenario, state, player.Id);

        if (requiredPercentage is not null)
        {
            WorldMapTerrain terrain = WorldHelpers.GetNativeTerrain(player.Race);
            int percentage = TerrainTransformCalculator.GetTransformedPercentage(state.Map, terrain);
            landInfo = new LandInfo(percentage, requiredPercentage.Value);
        }
        else
        {
            landInfo = null;
        }

        return CommandResult.Success(new Result(landInfo), []);
    }
}
