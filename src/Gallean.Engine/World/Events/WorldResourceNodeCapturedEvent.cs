namespace Gallean.Engine;

public sealed record WorldResourceNodeCapturedEvent(WorldResourceNodeId NodeId, WorldPlayerId NewOwnerId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
