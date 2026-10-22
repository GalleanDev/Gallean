namespace Gallean.Engine;

public sealed record WorldTurnResourcesGeneratedEvent(WorldPlayerId PlayerId, WorldResourceSet Resources) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
