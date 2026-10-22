using System.Collections.Immutable;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Mountains
    {
        public readonly record struct Input(
            ImmutableSortedDictionary<WorldMountainId, WorldMountain> Mountains,
            WorldVisibility Visibility);

        public static IReadOnlyList<MapObject> Build(Input input)
        {
            List<MapObject> objects = [];

            foreach (WorldMountain mountain in input.Mountains.Values)
            {
                if (IsObjectVisible(input.Visibility, mountain.Footprint))
                {
                    int size = Math.Max(mountain.Footprint.Width, mountain.Footprint.Height);

                    objects.Add(new Mountain(
                        mountain.Footprint.TopLeft,
                        GetFootprintDepth(mountain.Footprint, Layer.Mountains),
                        mountain.Footprint,
                        new AssetSpriteKey.Mountain(mountain.Race, size, mountain.Variant.Value)));
                }
            }

            return objects;
        }
    }
}
