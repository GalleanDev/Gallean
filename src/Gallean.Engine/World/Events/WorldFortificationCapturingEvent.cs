namespace Gallean.Engine;

public sealed record WorldFortificationCapturingEvent(
    WorldFortificationId FortificationId,
    WorldPlayerId PreviousOwnerId,
    WorldPlayerId NewOwnerId,
    WorldStackId CapturingStackId) : WorldEvent, IScopeBeginEvent<WorldFortificationCapturedEvent>
{
    public WorldFortificationCapturedEvent CreateScopeEndEvent()
    {
        return new WorldFortificationCapturedEvent(FortificationId, PreviousOwnerId, NewOwnerId, CapturingStackId);
    }

    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
