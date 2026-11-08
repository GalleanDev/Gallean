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

        private const int CloudSalt = 1;
        private const int PhaseSalt = 2;
        private const int CloudSize = 5;
        private const double CloudShare = 0.4;

        public static IReadOnlyList<MapObject> Build(Input input)
        {
            List<MapObject> objects = [];

            foreach (WorldMountain mountain in input.Mountains.Values)
            {
                if (IsObjectVisible(input.Visibility, mountain.Footprint))
                {
                    int size = Math.Max(mountain.Footprint.Width, mountain.Footprint.Height);
                    AssetSpriteKey.Mountain mountainKey = new(mountain.Race, size, mountain.Variant.Value);
                    double phase = GetFraction(mountain.Footprint.TopLeft, PhaseSalt);
                    bool hasCloud;

                    if (size == CloudSize && GetFraction(mountain.Footprint.TopLeft, CloudSalt) < CloudShare)
                    {
                        hasCloud = true;
                    }
                    else
                    {
                        hasCloud = false;
                    }

                    if (hasCloud)
                    {
                        objects.Add(new MountainCloud(
                            mountain.Footprint.TopLeft,
                            GetFootprintDepth(mountain.Footprint, Layer.MountainCloudsBack),
                            mountain.Footprint,
                            mountainKey,
                            phase,
                            MountainCloudLayer.Back));
                    }

                    objects.Add(new Mountain(
                        mountain.Footprint.TopLeft,
                        GetFootprintDepth(mountain.Footprint, Layer.Mountains),
                        mountain.Footprint,
                        mountainKey));

                    if (hasCloud)
                    {
                        objects.Add(new MountainCloud(
                            mountain.Footprint.TopLeft,
                            GetFootprintDepth(mountain.Footprint, Layer.MountainCloudsFront),
                            mountain.Footprint,
                            mountainKey,
                            phase,
                            MountainCloudLayer.Front));
                    }
                }
            }

            return objects;
        }

        private static double GetFraction(WorldPosition position, int salt)
        {
            return StableHash.Combine(StableHash.Combine(position.X, position.Y), salt) / (double)int.MaxValue;
        }
    }
}
