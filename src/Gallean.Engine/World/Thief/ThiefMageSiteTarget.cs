namespace Gallean.Engine;

internal sealed record ThiefMageSiteTarget(WorldMageSite Site)
    : ThiefTarget(OwnerId: null, Protection: 0);
