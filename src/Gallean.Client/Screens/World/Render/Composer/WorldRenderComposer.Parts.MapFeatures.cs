using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class MapFeatures
    {
        public readonly record struct Input(WorldMap Map, WorldVisibility Visibility);

        public static IReadOnlyList<MapObject> Build(Input input)
        {
            WorldMap map = input.Map;
            List<MapObject> objects = [];

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    WorldPosition position = new(x, y);

                    if (IsObjectVisible(input.Visibility, position))
                    {
                        WorldMapCell cell = map.GetCellAt(position);

                        MapTileFeature? item = cell.Feature switch
                        {
                            WorldMapFeature.Forest => new MapTileFeature(
                                position,
                                GetDepth(position, Layer.Trees),
                                new AssetSpriteKey.Tree(cell.Terrain, cell.FeatureVariant.Value)),
                            WorldMapFeature.None => null,
                            WorldMapFeature.Mountain => null,
                            WorldMapFeature.Road => null
                        };

                        if (item is not null)
                        {
                            objects.Add(item);
                        }
                    }
                }
            }

            return objects;
        }
    }
}
