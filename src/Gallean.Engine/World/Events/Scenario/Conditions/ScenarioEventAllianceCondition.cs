namespace Gallean.Engine;

public sealed record ScenarioEventAllianceCondition(WorldPlayerId Player1, WorldPlayerId Player2) : ScenarioEventCondition;
