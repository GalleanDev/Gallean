namespace Gallean.Engine;

internal interface IThiefTargetVisitor<in TContext, out TResult>
{
    TResult Visit(ThiefStackTarget target, TContext context);
    TResult Visit(ThiefFortificationTarget target, TContext context);
    TResult Visit(ThiefMerchantSiteTarget target, TContext context);
    TResult Visit(ThiefMageSiteTarget target, TContext context);
    TResult Visit(ThiefRuinTarget target, TContext context);
}
