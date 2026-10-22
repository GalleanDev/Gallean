namespace Gallean.Engine.Contracts;

public sealed record WorldRelation(
    WorldRelationId Id,
    int Current,
    int Previous,
    bool Alliance,
    int AllianceTurn,
    bool PermanentWar,
    bool PermanentAlliance);
