using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    public sealed record Terrains(IReadOnlyList<WorldMapTerrain> Values)
    {
        public static Terrains Build(WorldRenderState state)
        {
            List<WorldMapTerrain> terrains = [];

            Add(terrains, state.Terrain);

            if (state.Animation is TerrainExpansion terrainExpansion)
            {
                Add(terrains, terrainExpansion.OldTerrain);
            }

            return new Terrains(terrains);
        }

        private static void Add(List<WorldMapTerrain> terrains, MapTerrain terrain)
        {
            foreach (WorldMapTerrain groundTerrain in terrain.GroundTerrains)
            {
                if (!terrains.Contains(groundTerrain))
                {
                    terrains.Add(groundTerrain);
                }
            }

            foreach (MapTileBorder border in terrain.Borders)
            {
                if (!terrains.Contains(border.GroundTerrain))
                {
                    terrains.Add(border.GroundTerrain);
                }
            }
        }

        public bool Equals(Terrains? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Values.SequenceEqual(other.Values);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.AddSequence(Values);

            return hash.ToHashCode();
        }
    }
}
