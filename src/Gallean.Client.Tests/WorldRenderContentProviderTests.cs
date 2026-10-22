using System.Collections.Immutable;
using SkiaSharp;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client.Tests;

public sealed class WorldRenderContentProviderTests
{
    private static TestAssetProvider CreateAssets()
    {
        SKColor color = new(110, 120, 130, 255);
        TestAssetProviderBuilder builder = new();
        Sprite smallSprite = builder.CreateSprite(color, 64, 32);
        Sprite bigSprite = builder.CreateSprite(color, 96, 96);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 4, Width: 64, Height: 32, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(color, 64, 32), builder.CreateSprite(color, 64, 32), builder.CreateSprite(color, 64, 32), builder.CreateSprite(color, 64, 32)]);

        return builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? smallSprite : bigSprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();
    }

    private static WorldRenderState BuildState()
    {
        const int side = 6;
        List<MapTile> tiles = new(side * side);
        List<FogTile> fogTiles = new(side * side);
        List<MapObject> objects = [];
        ImmutableArray<WorldMapCell>.Builder cells = ImmutableArray.CreateBuilder<WorldMapCell>(side * side);

        for (int y = 0; y < side; y++)
        {
            for (int x = 0; x < side; x++)
            {
                WorldPosition position = new(x, y);
                bool isWater = (x + y * 3) % 4 == 0;

                if (isWater)
                {
                    tiles.Add(new MapTile(position, new MapTileSurface.Water(), RoadKey: null));
                    cells.Add(new WorldMapCell(WorldMapTerrain.Water, WorldMapFeature.None, new WorldObjectVariant(0)));
                }
                else
                {
                    tiles.Add(new MapTile(position, new MapTileSurface.Ground(WorldMapTerrain.Human), RoadKey: null));
                    cells.Add(new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)));
                    objects.Add(new MapTileFeature(position, position.X + position.Y, new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0)));
                }

                fogTiles.Add(new FogTile(position));
            }
        }

        WorldMap map = new(side, side, cells.MoveToImmutable());

        return new WorldRenderState(
            Terrain: new MapTerrain(map, tiles, [WorldMapTerrain.Human], []),
            ObjectiveMarkers: [], AmbientSounds: [],
            MapObjects: [.. objects.OrderBy(item => item.Depth)],
            Stacks: [],
            Animation: null,
            Clouds: [],
            FogTiles: fogTiles,
            SelectedPath: null,
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            Minimap: new MinimapState(side, side, [], []));
    }

    private static void CheckSameState(WorldRenderContent first, WorldRenderContent second)
    {
        Assert.That(second.GroundLayers, Is.SameAs(first.GroundLayers));
        Assert.That(second.Water, Is.SameAs(first.Water));
        Assert.That(second.BorderAtlas, Is.SameAs(first.BorderAtlas));
        Assert.That(second.RoadAtlas, Is.SameAs(first.RoadAtlas));
        Assert.That(second.ObjectAtlas, Is.SameAs(first.ObjectAtlas));
        Assert.That(second.ObjectSpriteDepths, Is.SameAs(first.ObjectSpriteDepths));
        Assert.That(second.DirectSprites, Is.SameAs(first.DirectSprites));
        Assert.That(second.Fog, Is.SameAs(first.Fog));
        Assert.That(second.MapClip, Is.SameAs(first.MapClip));
        Assert.That(second.MinimapImage, Is.SameAs(first.MinimapImage));
        Assert.That(second.HitTargets, Is.SameAs(first.HitTargets));
    }

    [Test]
    public void SameStateTest()
    {
        using TestAssetProvider assets = CreateAssets();
        WorldRenderContentProvider provider = new();
        WorldRenderState state = BuildState();

        WorldRenderContent first = provider.Get(state, assets);
        WorldRenderContent second = provider.Get(state, assets);

        CheckSameState(first, second);
    }

    [Test]
    public void HoverChangeTest()
    {
        using TestAssetProvider assets = CreateAssets();
        WorldRenderContentProvider provider = new();
        WorldRenderState state = BuildState();

        WorldRenderContent first = provider.Get(state, assets);
        WorldRenderState hovered = state with { Cursors = [new Cursor(new WorldPosition(1, 1), 1344, Theme.World.HoverCursorInnerColor, Theme.World.HoverCursorOuterColor, 1.0)] };
        WorldRenderContent second = provider.Get(hovered, assets);

        CheckSameState(first, second);
    }

    [Test]
    public void SelectionChangeTest()
    {
        using TestAssetProvider assets = CreateAssets();
        WorldRenderContentProvider provider = new();
        WorldRenderState state = BuildState();

        WorldRenderContent first = provider.Get(state, assets);
        WorldRenderState selected = state with
        {
            Reach = new ReachOverlay([new WorldPosition(2, 2), new WorldPosition(3, 2)]),
            Highlights = new HighlightOverlay(
            [
                new Highlight(
                    new Stack(
                        Position: new WorldPosition(2, 2),
                        Depth: 2439,
                        AssetKey: new AssetAnimationKey.StackUnit(new UnitDefinitionId("leader"), AssetAnimationKey.StackUnitPose.Stop, 7),
                        ShadowKey: null,
                        AuraKey: null,
                        BannerKey: new AssetSpriteKey.StackBanner(0),
                        Opacity: 1.0),
                    Theme.World.Highlight.SelectedColor)
            ])
        };
        WorldRenderContent second = provider.Get(selected, assets);

        CheckSameState(first, second);
    }

    [Test]
    public void RebuiltStateTest()
    {
        using TestAssetProvider assets = CreateAssets();
        WorldRenderContentProvider provider = new();

        WorldRenderContent first = provider.Get(BuildState(), assets);
        WorldRenderContent second = provider.Get(BuildState(), assets);

        Assert.That(second.Water, Is.SameAs(first.Water));
        Assert.That(second.ObjectAtlas, Is.Not.SameAs(first.ObjectAtlas));
    }

    [Test]
    public void TerrainLayoutChangeTest()
    {
        using TestAssetProvider assets = CreateAssets();
        WorldRenderContentProvider provider = new();
        WorldRenderState state = BuildState();

        WorldRenderContent first = provider.Get(state, assets);
        assets.RequiredAssets.Clear();

        List<MapTile> tiles = [.. state.Terrain.MapTiles];
        tiles[0] = new MapTile(new WorldPosition(0, 0), new MapTileSurface.Ground(WorldMapTerrain.Human), RoadKey: null);
        WorldRenderState expanded = state with
        {
            Terrain = state.Terrain with
            {
                MapTiles = tiles,
                Borders =
                [
                    new MapTileBorder.Blend(
                        new WorldPosition(0, 0),
                        new AssetSpriteKey.GroundBorder(
                            AssetSpriteKey.GroundBorderSet.Mask,
                            new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.North),
                            Variant: 0),
                        WorldMapTerrain.Human)
                ]
            }
        };
        WorldRenderContent second = provider.Get(expanded, assets);

        Assert.That(second.GroundLayers, Is.Not.SameAs(first.GroundLayers));
        Assert.That(second.BorderAtlas, Is.Not.SameAs(first.BorderAtlas));
        Assert.That(assets.RequiredAssets, Is.EqualTo<IReadOnlyList<AssetKey>>([
            new AssetAnimationKey.Water(),
            new AssetSpriteKey.GroundBorder(
                AssetSpriteKey.GroundBorderSet.Mask,
                new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.North),
                Variant: 0)
        ]));
    }
}
