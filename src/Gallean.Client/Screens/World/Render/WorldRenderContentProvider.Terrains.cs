using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    public sealed record Terrains(IReadOnlyList<WorldMapTerrain> Values)
    {
        public static Terrains Build(MapTerrain terrain)
        {
            List<WorldMapTerrain> terrains = [.. terrain.GroundTerrains];

            foreach (MapTileBorder border in terrain.Borders)
            {
                if (!terrains.Contains(border.GroundTerrain))
                {
                    terrains.Add(border.GroundTerrain);
                }
            }

            return new Terrains(terrains);
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
