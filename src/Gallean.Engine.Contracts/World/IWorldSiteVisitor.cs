namespace Gallean.Engine.Contracts;

public interface IWorldSiteVisitor<in TContext, out TResult>
{
    TResult Visit(WorldMerchantSite site, TContext context);
    TResult Visit(WorldMageSite site, TContext context);
    TResult Visit(WorldMercenarySite site, TContext context);
    TResult Visit(WorldTrainerSite site, TContext context);
}
