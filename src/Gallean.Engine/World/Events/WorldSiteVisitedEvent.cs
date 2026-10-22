namespace Gallean.Engine;

public sealed record WorldSiteVisitedEvent(
    WorldStackId StackId,
    WorldSiteId SiteId) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
