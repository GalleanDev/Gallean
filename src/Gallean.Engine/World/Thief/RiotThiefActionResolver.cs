namespace Gallean.Engine;

internal sealed class RiotThiefActionResolver(WorldThiefRiotAction action) : AbstractThiefActionResolver(action.TargetPosition)
{
    protected override AttemptResult AttemptAction(
        WorldState state,
        IEngineContext context,
        WorldStack thief,
        WorldPlayerId player,
        ThiefTarget target)
    {
        if (target is not ThiefFortificationTarget fortificationTarget)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        if (fortificationTarget.Fortification is not WorldCity city)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        int turns = CityCalculator.RollRiotTurns(context.Random, context.Globals);

        return AttemptResult.Success(
            WorldThiefActionType.RiotCity,
            fortificationTarget.Owner,
            [new WorldCityRiotStartedEvent(city.Id, turns)]);
    }
}
