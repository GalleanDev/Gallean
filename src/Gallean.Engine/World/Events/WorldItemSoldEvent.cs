namespace Gallean.Engine;

public sealed record WorldItemSoldEvent(
    WorldSiteId SiteId,
    WorldStackId StackId,
    WorldItem Item,
    WorldResourceSet Value) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
