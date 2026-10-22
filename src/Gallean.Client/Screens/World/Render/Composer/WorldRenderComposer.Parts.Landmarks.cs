using System.Collections.Immutable;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Landmarks
    {
        public readonly record struct Input(
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark> Landmarks,
            WorldMap Map,
            WorldVisibility Visibility);

        public static IReadOnlyList<MapObject> Build(Input input)
        {
            List<MapObject> objects = [];

            foreach (WorldLandmark landmark in input.Landmarks.Values)
            {
                if (IsObjectVisible(input.Visibility, landmark.Footprint))
                {
                    objects.Add(new Landmark(
                        landmark.Footprint.TopLeft,
                        GetFootprintDepth(landmark.Footprint, Layer.Landmarks),
                        landmark.Footprint,
                        GetAssetKey(input.Map, landmark)));
                }
            }

            return objects;
        }

        private static AssetAnimationKey GetAssetKey(WorldMap map, WorldLandmark landmark)
        {
            if (IsInWater(map, landmark.Footprint))
            {
                return new AssetAnimationKey.WaterLandmark(landmark.DefinitionId);
            }
            else
            {
                return new AssetAnimationKey.Landmark(landmark.DefinitionId);
            }
        }

        private static bool IsInWater(WorldMap map, WorldFootprint footprint)
        {
            int waterCount = 0;
            int tileCount = 0;

            foreach (WorldPosition tile in footprint.EnumerateTiles())
            {
                if (map.IsValidPosition(tile))
                {
                    tileCount++;

                    if (map.GetCellAt(tile).Terrain is WorldMapTerrain.Water)
                    {
                        waterCount++;
                    }
                }
            }

            return tileCount > 0 && waterCount * 2 > tileCount;
        }
    }
}
