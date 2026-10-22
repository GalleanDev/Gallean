namespace Gallean.Engine;

internal sealed class SpyRuinThiefActionResolver(WorldThiefSpyRuinAction action) : AbstractThiefActionResolver(action.TargetPosition)
{
    protected override AttemptResult AttemptAction(
        WorldState state,
        IEngineContext context,
        WorldStack thief,
        WorldPlayerId player,
        ThiefTarget target)
    {
        if (target is not ThiefRuinTarget ruinTarget)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        if (SpyCalculator.SpiesOnRuin(state, player, ruinTarget.Ruin.Id))
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetAlreadyKnown);
        }

        return AttemptResult.Success(
            WorldThiefActionType.SpyRuin,
            targetPlayerId: null,
            [new WorldRuinSpiedEvent(player, ruinTarget.Ruin.Id)]);
    }
}
