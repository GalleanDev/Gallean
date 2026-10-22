namespace Gallean.Engine;

public sealed record ScenarioStackTemplateDefinition(
    ScenarioStackTemplateId Id,
    WorldPlayerId OwnerId,
    WorldSubraceId SubraceId,
    UnitGrid<ScenarioStackTemplateUnit> Units,
    WorldCanonicalStackOrder Order,
    WorldStackDirection? Direction,
    int AiPriority);
