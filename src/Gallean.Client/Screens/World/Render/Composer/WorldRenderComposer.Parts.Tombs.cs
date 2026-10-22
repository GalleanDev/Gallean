using System.Collections.Immutable;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Tombs
    {
        public readonly record struct Input(
            ImmutableSortedDictionary<WorldTombId, WorldTomb> Tombs,
            WorldMap Map,
            WorldVisibility Visibility);

        public static IReadOnlyList<MapObject> Build(Input input)
        {
            List<MapObject> objects = [];
            HashSet<WorldPosition> positions = [];

            foreach (WorldTomb tomb in input.Tombs.Values)
            {
                if (positions.Add(tomb.Position) && IsObjectVisible(input.Visibility, tomb.Position))
                {
                    WorldMapCell cell = input.Map.GetCellAt(tomb.Position);
                    bool isWater = cell.Terrain == WorldMapTerrain.Water;

                    objects.Add(new Tomb(
                        tomb.Position,
                        GetDepth(tomb.Position, Layer.Tombs),
                        new AssetSpriteKey.Tombstone(isWater)));
                }
            }

            return objects;
        }
    }
}
