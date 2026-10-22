namespace Gallean.Engine;

public sealed record WorldTurnStartedEvent(WorldPlayerId PlayerId) : WorldEvent, IScopeEndEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
