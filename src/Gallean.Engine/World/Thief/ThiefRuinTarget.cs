namespace Gallean.Engine;

internal sealed record ThiefRuinTarget(WorldRuin Ruin)
    : ThiefTarget(OwnerId: null, Protection: 0);
