namespace Gallean.Engine.Contracts;

public sealed record WorldTomb(
    WorldTombId Id,
    WorldPosition Position,
    WorldPlayerId OwnerId,
    UnitDefinitionId LeaderDefinitionId,
    string? LeaderName,
    WorldPlayerId? DestroyerPlayerId,
    UnitDefinitionId? DestroyerLeaderDefinitionId,
    string? DestroyerLeaderName,
    int Turn);
