using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal static partial class WorldAssetPreloadPlan
{
    public static AssetPreloadPlan BuildWorld(WorldRenderState state, CapitalSceneViewModel? capitalScene)
    {
        List<AssetPreloadItem> required = [];
        List<AssetPreloadItem> optional = [];

        foreach (WorldMapTerrain terrain in GroundTerrains(state.Terrain.MapTiles))
        {
            required.Add(new AssetPreloadItem.Sprite(new AssetSpriteKey.Ground(terrain)));
        }

        required.Add(new AssetPreloadItem.Animation(new AssetAnimationKey.Water()));

        foreach (MapTile tile in state.Terrain.MapTiles)
        {
            if (tile.RoadKey is not null)
            {
                required.Add(new AssetPreloadItem.Sprite(tile.RoadKey));
            }
        }

        foreach (MapTileBorder border in state.Terrain.Borders)
        {
            switch (border)
            {
                case MapTileBorder.Blend blend:
                    required.Add(new AssetPreloadItem.Sprite(blend.MaskKey));
                    break;

                case MapTileBorder.Coast coast:
                    required.Add(new AssetPreloadItem.Sprite(coast.CoastKey));
                    required.Add(new AssetPreloadItem.Sprite(coast.MaskKey));
                    break;
            }
        }

        foreach (MapObject item in state.MapObjects)
        {
            AddObject(item);
        }

        foreach (MapObject item in state.Stacks)
        {
            AddObject(item);
        }

        if (state.Animation is not null)
        {
            state.Animation.Accept(new AnimationVisitor(required, optional), Empty.Value);
        }

        foreach (Cloud cloud in state.Clouds)
        {
            required.Add(new AssetPreloadItem.Animation(cloud.AssetKey));
        }

        if (capitalScene is not null)
        {
            foreach (CapitalSceneLayerViewModel layer in capitalScene.Layers)
            {
                if (layer.SpriteKey is not null)
                {
                    required.Add(new AssetPreloadItem.Sprite(layer.SpriteKey));
                }

                if (layer.AnimationKey is not null)
                {
                    required.Add(new AssetPreloadItem.Animation(layer.AnimationKey));
                }
            }
        }

        return MakePlan(required, optional);

        void AddObject(MapObject mapObject)
        {
            switch (mapObject)
            {
                case MapTileFeature feature:
                    required.Add(new AssetPreloadItem.Sprite(feature.AssetKey));
                    break;

                case ResourceNode resource:
                    required.Add(new AssetPreloadItem.Animation(resource.AssetKey));
                    break;

                case Bag bag:
                    required.Add(new AssetPreloadItem.Sprite(bag.AssetKey));
                    break;

                case Rod rod:
                    required.Add(new AssetPreloadItem.Sprite(rod.AssetKey));
                    break;

                case Tomb tomb:
                    required.Add(new AssetPreloadItem.Sprite(tomb.AssetKey));
                    break;

                case Fortification fortification:
                    required.Add(new AssetPreloadItem.Animation(fortification.AssetKey));
                    break;

                case Site site:
                    required.Add(new AssetPreloadItem.Animation(site.AssetKey));
                    break;

                case Landmark landmark:
                    required.Add(new AssetPreloadItem.Animation(landmark.AssetKey));
                    break;

                case ObjectShadow objectShadow:
                    required.Add(new AssetPreloadItem.Animation(objectShadow.AssetKey));
                    break;

                case Mountain mountain:
                    required.Add(new AssetPreloadItem.Sprite(mountain.AssetKey));
                    break;

                case LocationBanner locationBanner:
                    required.Add(new AssetPreloadItem.Animation(locationBanner.AssetKey));
                    break;

                case RuinBanner ruinBanner:
                    required.Add(new AssetPreloadItem.Sprite(ruinBanner.AssetKey));
                    break;

                case CityShield cityShield:
                    required.Add(new AssetPreloadItem.Sprite(cityShield.AssetKey));
                    break;

                case Stack stack:
                    required.Add(new AssetPreloadItem.Animation(stack.AssetKey));
                    required.Add(new AssetPreloadItem.Sprite(stack.BannerKey));
                    AddStackLayers(stack.ShadowKey, stack.AuraKey);
                    break;
            }
        }

        void AddStackLayers(AssetAnimationKey? shadowKey, AssetAnimationKey? auraKey)
        {
            if (shadowKey is not null)
            {
                required.Add(new AssetPreloadItem.Animation(shadowKey));
            }

            if (auraKey is not null)
            {
                required.Add(new AssetPreloadItem.Animation(auraKey));
            }
        }
    }

    public static AssetPreloadPlan BuildAnimation(WorldAnimation animation)
    {
        List<AssetPreloadItem> required = [];
        List<AssetPreloadItem> optional = [];

        animation.Accept(new AnimationVisitor(required, optional), Empty.Value);

        return MakePlan(required, optional);
    }

    private static AssetPreloadPlan MakePlan(List<AssetPreloadItem> required, List<AssetPreloadItem> optional)
    {
        return new AssetPreloadPlan([.. required.Distinct()], [.. optional.Distinct()], []);
    }

    private static IEnumerable<WorldMapTerrain> GroundTerrains(IReadOnlyList<MapTile> tiles)
    {
        return tiles
            .Select(tile => tile.Surface)
            .OfType<MapTileSurface.Ground>()
            .Select(ground => ground.Terrain)
            .Distinct();
    }
}
