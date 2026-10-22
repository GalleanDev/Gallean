using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    public sealed record WaterTiles(int MapWidth, int MapHeight, IReadOnlyList<WorldPosition> Tiles)
    {
        public static WaterTiles Build(MapTerrain terrain)
        {
            List<WorldPosition> tiles = [];

            foreach (MapTile tile in terrain.MapTiles)
            {
                if (tile.Surface is MapTileSurface.Water)
                {
                    tiles.Add(tile.Position);
                }
            }

            return new WaterTiles(terrain.Map.Width, terrain.Map.Height, tiles);
        }

        public bool Equals(WaterTiles? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return MapWidth == other.MapWidth &&
                   MapHeight == other.MapHeight &&
                   Tiles.SequenceEqual(other.Tiles);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add(MapWidth);
            hash.Add(MapHeight);
            hash.AddSequence(Tiles);

            return hash.ToHashCode();
        }
    }
}
