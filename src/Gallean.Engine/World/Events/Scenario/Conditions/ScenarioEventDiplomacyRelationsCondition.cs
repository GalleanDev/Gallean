namespace Gallean.Engine;

public sealed record ScenarioEventDiplomacyRelationsCondition(
    WorldPlayerId Player1,
    WorldPlayerId Player2,
    WorldRelationLevel Level) : ScenarioEventCondition;
