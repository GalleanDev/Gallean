namespace Gallean.Engine.Contracts;

public sealed record WorldTurnInfo(
    int TurnNumber,
    WorldPlayerId CurrentPlayerId);
