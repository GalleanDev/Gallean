namespace Gallean.Engine;

public sealed record WorldItemPurchasedEvent(
    WorldSiteId SiteId,
    WorldStackId StackId,
    WorldItem Item,
    WorldResourceSet Cost) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
