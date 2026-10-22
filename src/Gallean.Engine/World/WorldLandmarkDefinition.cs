namespace Gallean.Engine;

public sealed record WorldLandmarkDefinition(
    WorldLandmarkDefinitionId Id,
    GlobalTextId NameTextId,
    GlobalTextId DescriptionTextId,
    WorldLandmarkType Type,
    bool Mountain,
    int Width,
    int Height);
