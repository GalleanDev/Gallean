namespace Gallean.Engine;

internal sealed class SpyThiefActionResolver(WorldThiefSpyAction action) : AbstractThiefActionResolver(action.TargetPosition)
{
    private static AttemptResult AttemptSpy(WorldState state, WorldPlayerId player, WorldPlayerId ownerId)
    {
        if (SpyCalculator.SpiesOnPlayer(state, player, ownerId))
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetAlreadyKnown);
        }

        return AttemptResult.Success(
            WorldThiefActionType.Spy,
            ownerId,
            [new WorldSpyAddedEvent(player, ownerId)]);
    }

    protected override AttemptResult AttemptAction(
        WorldState state,
        IEngineContext context,
        WorldStack thief,
        WorldPlayerId player,
        ThiefTarget target)
    {
        if (target is ThiefStackTarget stackTarget)
        {
            return AttemptSpy(state, player, stackTarget.Stack.OwnerId);
        }
        else if (target is ThiefFortificationTarget fortificationTarget)
        {
            return AttemptSpy(state, player, fortificationTarget.Owner);
        }
        else
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }
    }
}
