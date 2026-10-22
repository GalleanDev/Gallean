using System.Collections.Immutable;
using SkiaSharp;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client.Tests;

public sealed class WorldOverlayRenderTests
{
    private static MapTerrain BuildTerrain()
    {
        const int side = 5;
        List<MapTile> tiles = [];
        ImmutableArray<WorldMapCell>.Builder cells = ImmutableArray.CreateBuilder<WorldMapCell>(side * side);

        for (int y = 0; y < side; y++)
        {
            for (int x = 0; x < side; x++)
            {
                tiles.Add(new MapTile(new WorldPosition(x, y), new MapTileSurface.Ground(WorldMapTerrain.Human), RoadKey: null));
                cells.Add(new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)));
            }
        }

        return new MapTerrain(new WorldMap(side, side, cells.MoveToImmutable()), tiles, [WorldMapTerrain.Human], []);
    }

    private static IReadOnlyList<SKColor> RenderColors(TestAssetProvider assets, WorldRenderState state, IReadOnlyList<SKPointI> points)
    {
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(256, 160, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(128, 0);

        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        using SKImage snapshot = surface.Snapshot();
        using SKPixmap pixels = snapshot.PeekPixels();
        List<SKColor> colors = [];

        foreach (SKPointI point in points)
        {
            colors.Add(pixels.GetPixelColor(point.X, point.Y));
        }

        return colors;
    }

    [Test]
    public void ReachTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Black, 64, 32);

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => groundSprite)
            .ResolveAnimation(_ => throw new InvalidOperationException("No animations."))
            .ResolveSound(_ => null)
            .Build();

        List<WorldPosition> reach = [];

        for (int y = 1; y <= 3; y++)
        {
            for (int x = 1; x <= 3; x++)
            {
                reach.Add(new WorldPosition(x, y));
            }
        }

        WorldRenderState state = new(Terrain: BuildTerrain(), FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [], Minimap: new MinimapState(5, 5, [], []), Cursors: [], Reach: new ReachOverlay(reach), Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);

        IReadOnlyList<SKColor> colors = RenderColors(assets, state, [new SKPointI(128, 10), new SKPointI(128, 24), new SKPointI(128, 26), new SKPointI(128, 29), new SKPointI(128, 64)]);

        Assert.That(colors, Is.EqualTo(new SKColor[]
        {
            new(0, 0, 0, 255), new(119, 154, 189, 255), new(48, 68, 92, 255), new(29, 42, 59, 255), new(18, 28, 40, 255)
        }));
    }

    [Test]
    public void HighlightTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Black, 64, 32);
        Sprite bannerSprite = builder.CreateSprite(SKColors.Transparent, 1, 1);
        Animation body = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 4, Height: 6, OffsetX: -3, OffsetY: -5, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.White, 4, 6, offsetX: -3, offsetY: -5)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? groundSprite : bannerSprite)
            .ResolveAnimation(_ => body)
            .ResolveSound(_ => null)
            .Build();

        Stack stack = new(
            new WorldPosition(2, 2),
            Depth: 0,
            new AssetAnimationKey.StackUnit(new UnitDefinitionId("g000uu0019"), AssetAnimationKey.StackUnitPose.Stop, 0),
            ShadowKey: null,
            AuraKey: null,
            new AssetSpriteKey.StackBanner(0),
            Opacity: 1.0);

        WorldRenderState state = new(Terrain: BuildTerrain(), FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [stack], Minimap: new MinimapState(5, 5, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([new Highlight(stack, Theme.World.Highlight.SelectedColor)]), SelectedPath: null, Animation: null, Clouds: []);

        IReadOnlyList<SKColor> colors = RenderColors(assets, state, [new SKPointI(126, 62), new SKPointI(124, 62), new SKPointI(121, 62), new SKPointI(116, 62), new SKPointI(105, 62)]);

        Assert.That(colors, Is.EqualTo(new SKColor[]
        {
            new(255, 255, 255, 255), new(106, 161, 234, 255), new(27, 41, 59, 255), new(2, 3, 4, 255), new(0, 0, 0, 255)
        }));
    }

    [Test]
    public void SingleTileReachTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Black, 64, 32);

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => groundSprite)
            .ResolveAnimation(_ => throw new InvalidOperationException("No animations."))
            .ResolveSound(_ => null)
            .Build();

        WorldRenderState state = new(Terrain: BuildTerrain(), FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [], Minimap: new MinimapState(5, 5, [], []), Cursors: [], Reach: new ReachOverlay([new WorldPosition(2, 2)]), Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);

        IReadOnlyList<SKColor> colors = RenderColors(assets, state, [new SKPointI(128, 40), new SKPointI(128, 56), new SKPointI(128, 58), new SKPointI(128, 64)]);

        Assert.That(colors, Is.EqualTo(new SKColor[]
        {
            new(0, 0, 0, 255), new(119, 154, 189, 255), new(48, 68, 92, 255), new(18, 28, 40, 255)
        }));
    }
}
