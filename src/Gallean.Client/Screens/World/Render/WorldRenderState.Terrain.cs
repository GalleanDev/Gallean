using Gallean.Common;

namespace Gallean.Client;

internal sealed partial record WorldRenderState
{
    public sealed record MapTerrain(
        WorldMap Map,
        IReadOnlyList<MapTile> MapTiles,
        IReadOnlyList<WorldMapTerrain> GroundTerrains,
        IReadOnlyList<MapTileBorder> Borders)
    {
        public bool Equals(MapTerrain? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Map == other.Map &&
                   MapTiles.SequenceEqual(other.MapTiles) &&
                   GroundTerrains.SequenceEqual(other.GroundTerrains) &&
                   Borders.SequenceEqual(other.Borders);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(Map);
            hash.AddSequence(MapTiles);
            hash.AddSequence(GroundTerrains);
            hash.AddSequence(Borders);

            return hash.ToHashCode();
        }
    }

    public sealed record MapTile(
        WorldPosition Position,
        MapTileSurface Surface,
        AssetSpriteKey? RoadKey);

    public abstract record MapTileBorder(
        WorldPosition Position,
        AssetSpriteKey MaskKey,
        WorldMapTerrain GroundTerrain)
    {
        public sealed record Blend(
            WorldPosition Position,
            AssetSpriteKey MaskKey,
            WorldMapTerrain GroundTerrain) : MapTileBorder(Position, MaskKey, GroundTerrain);

        public sealed record Coast(
            WorldPosition Position,
            AssetSpriteKey CoastKey,
            WorldMapTerrain GroundTerrain,
            AssetSpriteKey MaskKey) : MapTileBorder(Position, MaskKey, GroundTerrain);
    }

    public abstract record MapTileSurface
    {
        public sealed record Ground(
            WorldMapTerrain Terrain) : MapTileSurface;

        public sealed record Water : MapTileSurface;
    }
}
