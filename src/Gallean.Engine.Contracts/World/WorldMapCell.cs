namespace Gallean.Engine.Contracts;

public readonly record struct WorldMapCell(
    WorldMapTerrain Terrain,
    WorldMapFeature Feature,
    WorldObjectVariant FeatureVariant);
