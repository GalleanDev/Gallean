namespace Gallean.Engine;

public sealed record UnitUpgradeDefinition(
    UnitDefinitionId FromUnitId,
    UnitDefinitionId ToUnitId,
    WorldPlayerBuildingDefinitionId? RequiredBuildingId);
