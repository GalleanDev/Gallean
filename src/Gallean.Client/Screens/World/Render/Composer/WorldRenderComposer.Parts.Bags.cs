using System.Collections.Immutable;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Bags
    {
        public readonly record struct Input(
            ImmutableSortedDictionary<WorldBagId, WorldBag> Bags,
            WorldMap Map,
            WorldVisibility Visibility);

        public static IReadOnlyList<MapObject> Build(Input input)
        {
            List<MapObject> objects = [];

            foreach (WorldBag bag in input.Bags.Values)
            {
                if (IsObjectVisible(input.Visibility, bag.Position))
                {
                    WorldMapCell cell = input.Map.GetCellAt(bag.Position);
                    bool isWater = cell.Terrain == WorldMapTerrain.Water;

                    objects.Add(new Bag(
                        bag.Position,
                        GetDepth(bag.Position, Layer.Bags),
                        new AssetSpriteKey.Bag(isWater, bag.Variant.Value)));
                }
            }

            return objects;
        }
    }
}
