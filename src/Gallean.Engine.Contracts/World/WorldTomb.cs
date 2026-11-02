namespace Gallean.Engine.Contracts;

public sealed record WorldTomb(
    WorldTombId Id,
    WorldPosition Position,
    WorldPlayerId OwnerId,
    UnitDefinitionId LeaderDefinitionId,
    UnitName? LeaderName,
    WorldPlayerId? DestroyerPlayerId,
    UnitDefinitionId? DestroyerLeaderDefinitionId,
    UnitName? DestroyerLeaderName,
    int Turn);
