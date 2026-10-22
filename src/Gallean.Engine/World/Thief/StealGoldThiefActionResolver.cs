namespace Gallean.Engine;

internal sealed class StealGoldThiefActionResolver(WorldThiefStealGoldAction action) : AbstractThiefActionResolver(action.TargetPosition)
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

        WorldPlayerId ownerId = fortificationTarget.Owner;
        WorldPlayer owner = state.Players[ownerId];

        if (!WorldHelpers.IsRacePlayable(owner.Race))
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        int goldAvailable = owner.Resources.Gold;
        int goldStolen = context.Random.Next(goldAvailable * context.Globals.StealGoldPlayerPercent / 100) + 1;

        return AttemptResult.Success(
            WorldThiefActionType.StealGold,
            ownerId,
            [new WorldResourcesStolenEvent(thief.Id, ownerId, new WorldResourceSet(Math.Min(goldStolen, goldAvailable), 0, 0, 0, 0, 0))]);
    }
}
