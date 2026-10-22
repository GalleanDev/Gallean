namespace Gallean.Engine;

public sealed record WorldAiActionEndedEvent(WorldPlayerId PlayerId, WorldAiMemory Memory) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
