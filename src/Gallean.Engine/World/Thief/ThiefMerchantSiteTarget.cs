namespace Gallean.Engine;

internal sealed record ThiefMerchantSiteTarget(WorldMerchantSite Site)
    : ThiefTarget(OwnerId: null, Protection: 0)
{
    public override TResult Accept<TContext, TResult>(IThiefTargetVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
