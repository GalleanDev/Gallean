namespace Gallean.Engine.Contracts;

public sealed record WorldRod(
    WorldRodId Id,
    WorldPlayerId OwnerId,
    WorldPosition Position);
