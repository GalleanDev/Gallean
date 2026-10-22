namespace Gallean.Engine;

public sealed record WorldTurnStartingEvent(WorldPlayerId PlayerId) : WorldEvent, IScopeBeginEvent<WorldTurnStartedEvent>
{
    public WorldTurnStartedEvent CreateScopeEndEvent()
    {
        return new WorldTurnStartedEvent(PlayerId);
    }

    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
