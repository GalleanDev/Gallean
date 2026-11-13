namespace Gallean.Engine;

internal sealed record ThiefMerchantSiteTarget(WorldMerchantSite Site)
    : ThiefTarget(OwnerId: null, Protection: 0);
