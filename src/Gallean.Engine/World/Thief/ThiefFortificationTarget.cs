namespace Gallean.Engine;

internal sealed record ThiefFortificationTarget(WorldFortification Fortification, WorldPlayerId Owner, int Protection)
    : ThiefTarget(Owner, Protection);
