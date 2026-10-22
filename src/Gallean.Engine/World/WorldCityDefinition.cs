namespace Gallean.Engine;

public sealed record WorldCityDefinition(
    WorldCityDefinitionId Id,
    int Level,
    int VisionRadius,
    int Regeneration,
    WorldResourceSet UpgradeCost);
