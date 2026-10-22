namespace Gallean.Engine;

internal sealed class BribeThiefActionResolver(WorldThiefBribeAction action) : AbstractThiefActionResolver(action.TargetPosition)
{
    protected override AttemptResult AttemptAction(
        WorldState state,
        IEngineContext context,
        WorldStack thief,
        WorldPlayerId player,
        ThiefTarget target)
    {
        WorldPlan plan = WorldPlan.For(state);

        if (target is not ThiefFortificationTarget fortificationTarget)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        if (fortificationTarget.Fortification is not WorldCity city)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        if (state.Players[city.OwnerId].Race != WorldRace.Neutral)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        WorldResourceSet cost = ThiefCalculator.GetCityBribeCost(city, state, context);

        if (cost.Gold <= 0)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        WorldPlayer thiefPlayer = state.Players[player];

        if (!ResourceSetCalculator.CanAfford(thiefPlayer.Resources, cost))
        {
            return AttemptResult.Fail(WorldCommandError.InsufficientResources);
        }

        WorldStack? stack = plan.GetStackAt(city.Entrance);
        List<WorldEvent> events = [];

        if (stack is not null)
        {
            events.Add(new WorldStackDestroyedEvent(stack.Id, new WorldUnitGroup.Stack(thief.Id), thief.OwnerId));
        }

        events.Add(new WorldCityBribedEvent(thief.Id, city.Id, cost));
        events.Add(new WorldFortificationCapturingEvent(city.Id, city.OwnerId, player, thief.Id));

        return AttemptResult.Success(WorldThiefActionType.Bribe, fortificationTarget.Owner, events);
    }
}
