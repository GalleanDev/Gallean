namespace Gallean.Engine.Contracts;

public sealed record WorldLandmark(
    WorldLandmarkId Id,
    WorldLandmarkDefinitionId DefinitionId,
    WorldFootprint Footprint);
