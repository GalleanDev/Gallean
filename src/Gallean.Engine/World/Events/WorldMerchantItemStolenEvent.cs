namespace Gallean.Engine;

public sealed record WorldMerchantItemStolenEvent(
    WorldStackId StackId,
    WorldSiteId SiteId,
    WorldItem Item) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
