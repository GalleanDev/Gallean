namespace Gallean.Engine;

public sealed record WorldMercenaryPurchasedEvent(
    WorldSiteId SiteId,
    WorldStackId StackId,
    WorldUnit Unit,
    GridPosition Position,
    WorldResourceSet Cost) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
