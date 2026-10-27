using Avalonia;
using Avalonia.Skia;
using Gallean.Common;
using Gallean.Tests.Common;
using SkiaSharp;
using static Gallean.Client.WorldRenderContent;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client.Tests;

public sealed class WorldObjectRenderTests
{
    private static TestAssetProvider CreateAssetProvider()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite objectSprite = builder.CreateSprite(SKColors.Red, 2, 2);
        Sprite bannerSprite = builder.CreateSprite(SKColors.Transparent, 1, 1);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 2, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 4, 4), builder.CreateSprite(SKColors.Lime, 4, 4)]);

        return builder
            .ResolveSprite(key => key switch
            {
                AssetSpriteKey.Ground => groundSprite,
                AssetSpriteKey.StackBanner => bannerSprite,
                _ => objectSprite
            })
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();
    }

    private static IReadOnlyList<SKColor> ReadColors(SKSurface surface, IReadOnlyList<SKPointI> points)
    {
        using SKImage snapshot = surface.Snapshot();
        using SKPixmap pixels = snapshot.PeekPixels();
        List<SKColor> colors = [];

        foreach (SKPointI point in points)
        {
            colors.Add(pixels.GetPixelColor(point.X, point.Y));
        }

        return colors;
    }

    private static int CountColor(SKSurface surface, SKColor color)
    {
        using SKImage snapshot = surface.Snapshot();
        using SKPixmap pixels = snapshot.PeekPixels();
        int count = 0;

        for (int y = 0; y < pixels.Height; y++)
        {
            for (int x = 0; x < pixels.Width; x++)
            {
                if (pixels.GetPixelColor(x, y) == color)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static SKColor RenderMoveOverBag(TestAssetProvider assets, MapTerrain terrain, StackMove stackMove, int bagDepth)
    {
        Bag bag = new(new WorldPosition(0, 0), bagDepth, new AssetSpriteKey.Bag(IsWater: false, 0));

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [bag], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: stackMove, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(256, 256, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(64, 64);

        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, stackMove.Duration / 2.0, renderResolution: 1.0f, hitboxOverlay: false);

        IReadOnlyList<SKColor> colors = ReadColors(surface, [new SKPointI(81, 73)]);

        return colors[0];
    }

    private static SKColor RenderEffectOverBag(TestAssetProvider assets, MapTerrain terrain, WorldAnimation animation, int bagDepth)
    {
        Bag bag = new(new WorldPosition(0, 0), bagDepth, new AssetSpriteKey.Bag(IsWater: false, 0));

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [bag], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: animation, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(128, 128, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(64, 64);

        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        IReadOnlyList<SKColor> colors = ReadColors(surface, [new SKPointI(64, 64)]);

        return colors[0];
    }

    private static SKColor RenderStackOverBag(TestAssetProvider assets, MapTerrain terrain, int stackDepth, int bagDepth)
    {
        Bag bag = new(new WorldPosition(0, 0), bagDepth, new AssetSpriteKey.Bag(IsWater: false, 0));
        Stack stack = new(new WorldPosition(0, 0), stackDepth, new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Stop, 0), ShadowKey: null, AuraKey: null, new AssetSpriteKey.StackBanner(4), Opacity: 1.0);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [bag], Stacks: [stack], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(256, 256, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(64, 64);

        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        IReadOnlyList<SKColor> colors = ReadColors(surface, [new SKPointI(65, 65)]);

        return colors[0];
    }

    private static SKColor RenderObjectsOverTile(TestAssetProvider assets, MapTerrain terrain, IReadOnlyList<MapObject> objects)
    {
        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: objects, Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(128, 128, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(64, 64);

        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        IReadOnlyList<SKColor> colors = ReadColors(surface, [new SKPointI(64, 64)]);

        return colors[0];
    }

    private static SKColor RenderSpellOverBag(TestAssetProvider assets, MapTerrain terrain, Bag bag, SpellCast spellCast)
    {
        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [bag], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: spellCast, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(128, 128, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Translate(64, 64);
        surface.Canvas.Clear(SKColors.Transparent);

        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        IReadOnlyList<SKColor> colors = ReadColors(surface, [new SKPointI(64, 64)]);

        return colors[0];
    }

    [Test]
    public void StaticAndAnimatedObjectsTest()
    {
        using TestAssetProvider assets = CreateAssetProvider();
        IReadOnlyList<MapObject> objects =
        [
            new MapTileFeature(
                new WorldPosition(6, 2),
                Depth: 8,
                new AssetSpriteKey.Tree(WorldMapTerrain.Human, 0)),
            new ResourceNode(
                new WorldPosition(8, 2),
                Depth: 10,
                new AssetAnimationKey.Crystal(WorldResourceType.Gold))
        ];
        IReadOnlyList<MapObject> stacks =
        [
            new Stack(
                new WorldPosition(7, 2),
                Depth: 9,
                new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Stop, 0),
                ShadowKey: null,
                AuraKey: null,
                new AssetSpriteKey.StackBanner(4),
                Opacity: 1.0)
        ];

        WorldPosition position = new(0, 0);

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    position,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);
        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: objects, Stacks: stacks, Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);
        SKImageInfo info = new(256, 256, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        IReadOnlyList<SKPointI> samplePoints =
        [
            new(128, 128),
            new(162, 146),
            new(192, 160)
        ];

        surface.Canvas.Clear(SKColors.Transparent);
        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);
        IReadOnlyList<SKColor> firstFrame = ReadColors(surface, samplePoints);

        surface.Canvas.Clear(SKColors.Transparent);
        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.FromSeconds(0.1), TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);
        IReadOnlyList<SKColor> secondFrame = ReadColors(surface, samplePoints);

        Assert.That(firstFrame, Is.EqualTo([SKColors.Red, SKColors.Blue, SKColors.Blue]));
        Assert.That(secondFrame, Is.EqualTo([SKColors.Red, SKColors.Lime, SKColors.Lime]));
    }

    [Test]
    public void LandmarkAnimationTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Animation landmarkAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 2, Width: 4, Height: 4, OffsetX: -2, OffsetY: -20, TriggerFrame: null, SecondTriggerFrame: null),
            [
                builder.CreateSprite(SKColors.Blue, 4, 4, offsetX: -2, offsetY: -20),
                builder.CreateSprite(SKColors.Lime, 4, 4, offsetX: -2, offsetY: -20)
            ]);

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => groundSprite)
            .ResolveAnimation(_ => landmarkAnimation)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        WorldPosition position = new(0, 0);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    position,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Landmark landmark = new(
            position,
            Depth: 0,
            new WorldFootprint(position, 1, 1),
            new AssetAnimationKey.Landmark(new WorldLandmarkDefinitionId("lmark1")));

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [landmark], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(128, 128, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Translate(64, 64);

        IReadOnlyList<SKPointI> samplePoints = [new(63, 45)];

        surface.Canvas.Clear(SKColors.Transparent);
        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);
        IReadOnlyList<SKColor> firstFrame = ReadColors(surface, samplePoints);

        surface.Canvas.Clear(SKColors.Transparent);
        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.FromSeconds(0.1), TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);
        IReadOnlyList<SKColor> secondFrame = ReadColors(surface, samplePoints);

        Assert.That(firstFrame, Is.EqualTo([SKColors.Blue]));
        Assert.That(secondFrame, Is.EqualTo([SKColors.Lime]));
    }

    [Test]
    public void LandmarkFrameBoundsTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Animation landmarkAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 2, Width: 8, Height: 4, OffsetX: -2, OffsetY: -20, TriggerFrame: null, SecondTriggerFrame: null),
            [
                builder.CreateSprite(SKColors.Blue, 4, 4, offsetX: -2, offsetY: -20),
                builder.CreateSprite(SKColors.Lime, 4, 4, offsetX: 2, offsetY: -20)
            ]);

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => groundSprite)
            .ResolveAnimation(_ => landmarkAnimation)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        WorldPosition position = new(0, 0);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    position,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Landmark landmark = new(
            position,
            Depth: 0,
            new WorldFootprint(position, 1, 1),
            new AssetAnimationKey.Landmark(new WorldLandmarkDefinitionId("lmark1")));

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [landmark], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(128, 128, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Translate(64, 64);

        IReadOnlyList<SKPointI> samplePoints = [new(63, 45), new(67, 45)];

        surface.Canvas.Clear(SKColors.Transparent);
        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);
        IReadOnlyList<SKColor> firstFrame = ReadColors(surface, samplePoints);

        surface.Canvas.Clear(SKColors.Transparent);
        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.FromSeconds(0.1), TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);
        IReadOnlyList<SKColor> secondFrame = ReadColors(surface, samplePoints);

        SKColor backdrop = Theme.World.BackdropColor.ToSKColor();

        Assert.That(firstFrame, Is.EqualTo([SKColors.Blue, backdrop]));
        Assert.That(secondFrame, Is.EqualTo([backdrop, SKColors.Lime]));
    }

    [Test]
    public void StackHitTargetTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite bannerSprite = builder.CreateSprite(SKColors.Transparent, 1, 1);
        Animation body = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 4, Height: 6, OffsetX: -3, OffsetY: -5, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 4, 6, offsetX: -3, offsetY: -5)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? groundSprite : bannerSprite)
            .ResolveAnimation(_ => body)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        WorldPosition tilePosition = new(0, 0);
        WorldPosition stackPosition = new(7, 2);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    tilePosition,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Stack stack = new(
            stackPosition,
            Depth: 0,
            new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Stop, 0),
            ShadowKey: null,
            AuraKey: null,
            new AssetSpriteKey.StackBanner(4),
            Opacity: 1.0);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [stack], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);
        WorldRenderContentProvider provider = new();

        WorldRenderContent content = provider.Get(state, assets);

        Rect bodyBounds = new(157, 139, 4, 6);

        Assert.That(content.DirectSprites, Is.Empty);
        Assert.That(content.Stacks, Is.EqualTo([new StackSprite(stack, bodyBounds)]));

        ImmutableBitmap hitMask = Assert.NotNull(body.HitMask);

        Assert.That(content.HitTargets, Is.EqualTo([
            new HitTarget(stackPosition, bodyBounds, hitMask)
        ]));
    }

    [Test]
    public void StackAuraTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite bannerSprite = builder.CreateSprite(SKColors.Transparent, 1, 1);
        Animation body = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 4, Height: 6, OffsetX: -3, OffsetY: -5, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 4, 6, offsetX: -3, offsetY: -5)]);
        Animation aura = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 10, Height: 10, OffsetX: -6, OffsetY: -8, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Lime, 10, 10, offsetX: -6, offsetY: -8)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? groundSprite : bannerSprite)
            .ResolveAnimation(key => key is AssetAnimationKey.StackUnitAura ? aura : body)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        WorldPosition tilePosition = new(0, 0);
        WorldPosition stackPosition = new(7, 2);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    tilePosition,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        UnitDefinitionId unitDefinitionId = new("g000uu5130");

        Stack stack = new(
            stackPosition,
            Depth: 0,
            new AssetAnimationKey.StackUnit(unitDefinitionId, AssetAnimationKey.StackUnitPose.Stop, 0),
            new AssetAnimationKey.StackUnitShadow(unitDefinitionId, AssetAnimationKey.StackUnitPose.Stop, 0),
            new AssetAnimationKey.StackUnitAura(unitDefinitionId, AssetAnimationKey.StackUnitPose.Stop, 0),
            new AssetSpriteKey.StackBanner(4),
            Opacity: 1.0);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [stack], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);
        WorldRenderContentProvider provider = new();

        WorldRenderContent content = provider.Get(state, assets);

        Assert.That(content.DirectSprites, Is.Empty);
        Assert.That(content.Stacks, Is.EqualTo([new StackSprite(stack, new Rect(154, 136, 10, 10))]));
    }

    [Test]
    public void StackMoveTest()
    {
        using TestAssetProvider assets = CreateAssetProvider();

        WorldMap map = new(
            Width: 2,
            Height: 1,
            Cells:
            [
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)),
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))
            ]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    new WorldPosition(0, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null),
                new MapTile(
                    new WorldPosition(1, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        StackMove stackMove = new(
            new WorldStackId("Stack1"),
            new AssetSoundKey.StackUnitMove(new UnitDefinitionId("Leader"), Variant: 0),
            new AssetSpriteKey.StackBanner(4),
            [
                new StackMoveStep(new WorldPosition(0, 0), 3, new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Move, 0), ShadowKey: null, AuraKey: null, TimeSpan.Zero),
                new StackMoveStep(new WorldPosition(1, 0), 4, new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Move, 0), ShadowKey: null, AuraKey: null, TimeSpan.FromMilliseconds(200))
            ],
            Opacity: 1.0,
            IsObjective: false,
            Highlight: null);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: stackMove, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(256, 256, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(64, 64);

        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, stackMove.Duration / 2.0, renderResolution: 1.0f, hitboxOverlay: false);

        IReadOnlyList<SKColor> midFlight = ReadColors(
            surface,
            [new SKPointI(81, 73)]);

        Assert.That(midFlight, Is.EqualTo([SKColors.Blue]));

        surface.Canvas.Clear(SKColors.Transparent);
        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.FromSeconds(2), renderResolution: 1.0f, hitboxOverlay: false);

        IReadOnlyList<SKColor> arrived = ReadColors(
            surface,
            [new SKPointI(97, 81), new SKPointI(81, 73)]);

        Assert.That(arrived, Is.EqualTo([SKColors.Blue, SKColors.Empty]));
    }

    [Test]
    public void MovingStackBannerTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite bannerSprite = builder.CreateSprite(SKColors.Yellow, 4, 4, offsetX: 8, offsetY: 0);
        Animation body = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 2, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 4, 4), builder.CreateSprite(SKColors.Lime, 4, 4)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.StackBanner ? bannerSprite : groundSprite)
            .ResolveAnimation(_ => body)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 2,
            Height: 1,
            Cells:
            [
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)),
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))
            ]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    new WorldPosition(0, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null),
                new MapTile(
                    new WorldPosition(1, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        StackMove stackMove = new(
            new WorldStackId("Stack1"),
            new AssetSoundKey.StackUnitMove(new UnitDefinitionId("Leader"), Variant: 0),
            new AssetSpriteKey.StackBanner(4),
            [
                new StackMoveStep(new WorldPosition(0, 0), 3, new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Move, 0), ShadowKey: null, AuraKey: null, TimeSpan.Zero),
                new StackMoveStep(new WorldPosition(1, 0), 4, new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Move, 0), ShadowKey: null, AuraKey: null, TimeSpan.FromMilliseconds(200))
            ],
            Opacity: 1.0,
            IsObjective: false,
            Highlight: null);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: stackMove, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(256, 256, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(64, 64);

        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, stackMove.Duration / 2.0, renderResolution: 1.0f, hitboxOverlay: false);

        IReadOnlyList<SKColor> midFlight = ReadColors(
            surface,
            [new SKPointI(81, 73), new SKPointI(89, 73)]);

        Assert.That(midFlight, Is.EqualTo([SKColors.Blue, SKColors.Yellow]));

        surface.Canvas.Clear(SKColors.Transparent);
        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.FromSeconds(2), renderResolution: 1.0f, hitboxOverlay: false);

        IReadOnlyList<SKColor> arrived = ReadColors(
            surface,
            [new SKPointI(97, 81), new SKPointI(105, 81), new SKPointI(89, 73)]);

        Assert.That(arrived, Is.EqualTo([SKColors.Blue, SKColors.Yellow, SKColors.Empty]));
    }

    [Test]
    public void MovingStackDepthTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite bagSprite = builder.CreateSprite(SKColors.Red, 128, 128, offsetX: -64, offsetY: -64);
        Sprite bannerSprite = builder.CreateSprite(SKColors.Transparent, 1, 1);
        Animation body = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 2, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 4, 4), builder.CreateSprite(SKColors.Lime, 4, 4)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key switch
            {
                AssetSpriteKey.Ground => groundSprite,
                AssetSpriteKey.StackBanner => bannerSprite,
                _ => bagSprite
            })
            .ResolveAnimation(_ => body)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 2,
            Height: 1,
            Cells:
            [
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)),
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))
            ]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    new WorldPosition(0, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null),
                new MapTile(
                    new WorldPosition(1, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        StackMove stackMove = new(
            new WorldStackId("Stack1"),
            new AssetSoundKey.StackUnitMove(new UnitDefinitionId("Leader"), Variant: 0),
            new AssetSpriteKey.StackBanner(4),
            [
                new StackMoveStep(new WorldPosition(0, 0), 3, new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Move, 0), ShadowKey: null, AuraKey: null, TimeSpan.Zero),
                new StackMoveStep(new WorldPosition(1, 0), 4, new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Move, 0), ShadowKey: null, AuraKey: null, TimeSpan.FromMilliseconds(200))
            ],
            Opacity: 1.0,
            IsObjective: false,
            Highlight: null);

        Assert.That(RenderMoveOverBag(assets, terrain, stackMove, bagDepth: 3), Is.EqualTo(SKColors.Blue));
        Assert.That(RenderMoveOverBag(assets, terrain, stackMove, bagDepth: 4), Is.EqualTo(new SKColor(128, 0, 128, 255)));
        Assert.That(RenderMoveOverBag(assets, terrain, stackMove, bagDepth: 5), Is.EqualTo(new SKColor(128, 0, 128, 255)));
    }

    [Test]
    public void StackDepthTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite bagSprite = builder.CreateSprite(SKColors.Red, 128, 128, offsetX: -64, offsetY: -64);
        Sprite bannerSprite = builder.CreateSprite(SKColors.Transparent, 1, 1);
        Animation body = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 2, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 4, 4), builder.CreateSprite(SKColors.Lime, 4, 4)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key switch
            {
                AssetSpriteKey.Ground => groundSprite,
                AssetSpriteKey.StackBanner => bannerSprite,
                _ => bagSprite
            })
            .ResolveAnimation(_ => body)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    new WorldPosition(0, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Assert.That(RenderStackOverBag(assets, terrain, stackDepth: 3, bagDepth: 5), Is.EqualTo(new SKColor(128, 0, 128, 255)));
        Assert.That(RenderStackOverBag(assets, terrain, stackDepth: 5, bagDepth: 3), Is.EqualTo(SKColors.Blue));
    }

    [Test]
    public void MovingStackShadowTest()
    {
        SKColor shadowColor = new(64, 64, 64);
        AssetAnimationKey boatShadowKey = new AssetAnimationKey.StackBoatShadow(WorldRace.Human, AssetAnimationKey.StackUnitPose.Move, 0);

        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Animation body = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 2, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 4, 4), builder.CreateSprite(SKColors.Lime, 4, 4)]);
        Animation shadow = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(shadowColor, 40, 16, offsetX: -18, offsetY: 6)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => groundSprite)
            .ResolveAnimation(key => key is AssetAnimationKey.StackBoatShadow ? shadow : body)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 2,
            Height: 1,
            Cells:
            [
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)),
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))
            ]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    new WorldPosition(0, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null),
                new MapTile(
                    new WorldPosition(1, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        StackMove stackMove = new(
            new WorldStackId("Stack1"),
            new AssetSoundKey.StackUnitMove(new UnitDefinitionId("Leader"), Variant: 0),
            new AssetSpriteKey.StackBanner(4),
            [
                new StackMoveStep(new WorldPosition(0, 0), 3, new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Move, 0), boatShadowKey, AuraKey: null, TimeSpan.Zero),
                new StackMoveStep(new WorldPosition(1, 0), 4, new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Move, 0), boatShadowKey, AuraKey: null, TimeSpan.FromMilliseconds(200))
            ],
            Opacity: 1.0,
            IsObjective: false,
            Highlight: null);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: stackMove, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(256, 256, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(64, 64);

        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, stackMove.Duration / 2.0, renderResolution: 1.0f, hitboxOverlay: false);

        IReadOnlyList<SKColor> bodyPixels = ReadColors(surface, [new SKPointI(81, 73)]);
        int shadowPixels = CountColor(surface, shadowColor);

        Assert.That(bodyPixels, Is.EqualTo([SKColors.Blue]));
        Assert.That(shadowPixels, Is.EqualTo(640));
    }

    [Test]
    public void FogOverObjectsTest()
    {
        using TestAssetProvider assets = CreateAssetProvider();

        WorldMap map = new(
            Width: 3,
            Height: 1,
            Cells:
            [
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)),
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)),
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))
            ]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    new WorldPosition(0, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null),
                new MapTile(
                    new WorldPosition(1, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null),
                new MapTile(
                    new WorldPosition(2, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        IReadOnlyList<MapObject> objects =
        [
            new MapTileFeature(new WorldPosition(0, 0), Depth: 0, new AssetSpriteKey.Tree(WorldMapTerrain.Human, 0)),
            new MapTileFeature(new WorldPosition(2, 0), Depth: 1, new AssetSpriteKey.Tree(WorldMapTerrain.Human, 0))
        ];

        WorldRenderState state = new(
            Terrain: terrain,
            FogTiles:
            [
                new FogTile(new WorldPosition(1, 0)),
                new FogTile(new WorldPosition(2, 0))
            ],
            MapObjects: objects,
            Stacks: [],
            Minimap: new MinimapState(1, 1, [], []),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(256, 256, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(64, 64);

        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        IReadOnlyList<SKColor> colors = ReadColors(surface, [new SKPointI(64, 64), new SKPointI(128, 96)]);

        Assert.That(colors, Is.EqualTo([SKColors.Red, SKColors.Black]));
    }

    [Test]
    public void DeepFogMapEdgeTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite tallSprite = builder.CreateSprite(SKColors.Red, 8, 96, offsetX: -4, offsetY: -48);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 2, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 4, 4), builder.CreateSprite(SKColors.Lime, 4, 4)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? groundSprite : tallSprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    new WorldPosition(0, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        IReadOnlyList<MapObject> objects =
        [
            new MapTileFeature(new WorldPosition(0, 0), Depth: 0, new AssetSpriteKey.Tree(WorldMapTerrain.Human, 0))
        ];

        WorldRenderState foggedState = new(
            Terrain: terrain,
            FogTiles: [new FogTile(new WorldPosition(0, 0))],
            MapObjects: objects,
            Stacks: [],
            Minimap: new MinimapState(1, 1, [], []),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        WorldRenderState exploredState = new(
            Terrain: terrain,
            FogTiles: [],
            MapObjects: objects,
            Stacks: [],
            Minimap: new MinimapState(1, 1, [], []),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        WorldRenderer renderer = new();
        SKImageInfo info = new(256, 256, SKColorType.RgbaF32, SKAlphaType.Premul);

        using SKSurface foggedSurface = SKSurface.Create(info);
        foggedSurface.Canvas.Clear(SKColors.Transparent);
        foggedSurface.Canvas.Translate(64, 64);
        renderer.Render(foggedSurface.Canvas, new WorldRenderContentProvider().Get(foggedState, assets), foggedState, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        using SKSurface exploredSurface = SKSurface.Create(info);
        exploredSurface.Canvas.Clear(SKColors.Transparent);
        exploredSurface.Canvas.Translate(64, 64);
        renderer.Render(exploredSurface.Canvas, new WorldRenderContentProvider().Get(exploredState, assets), exploredState, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        Assert.That(ReadColors(foggedSurface, [new SKPointI(64, 40), new SKPointI(64, 64)]), Is.EqualTo([Theme.World.BackdropColor.ToSKColor(), SKColors.Black]));
        Assert.That(ReadColors(exploredSurface, [new SKPointI(64, 40), new SKPointI(64, 64)]), Is.EqualTo([SKColors.Red, SKColors.Red]));
    }

    [Test]
    public void FadedFogMapEdgeTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite tallSprite = builder.CreateSprite(SKColors.Red, 8, 96, offsetX: -4, offsetY: -48);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 2, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 4, 4), builder.CreateSprite(SKColors.Lime, 4, 4)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? groundSprite : tallSprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 2,
            Height: 1,
            Cells:
            [
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)),
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))
            ]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    new WorldPosition(0, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null),
                new MapTile(
                    new WorldPosition(1, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        IReadOnlyList<MapObject> objects =
        [
            new MapTileFeature(new WorldPosition(0, 0), Depth: 0, new AssetSpriteKey.Tree(WorldMapTerrain.Human, 0))
        ];

        WorldRenderState state = new(
            Terrain: terrain,
            FogTiles: [new FogTile(new WorldPosition(0, 0))],
            MapObjects: objects,
            Stacks: [],
            Minimap: new MinimapState(1, 1, [], []),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        WorldRenderer renderer = new();
        SKImageInfo info = new(256, 256, SKColorType.RgbaF32, SKAlphaType.Premul);

        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(64, 64);
        renderer.Render(surface.Canvas, new WorldRenderContentProvider().Get(state, assets), state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        Assert.That(ReadColors(surface, [new SKPointI(64, 40)]), Is.EqualTo([Theme.World.BackdropColor.ToSKColor()]));
    }

    [Test]
    public void ExploredMapEdgeTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite tallSprite = builder.CreateSprite(SKColors.Red, 8, 96, offsetX: -4, offsetY: -48);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 2, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 4, 4), builder.CreateSprite(SKColors.Lime, 4, 4)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? groundSprite : tallSprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 3,
            Height: 1,
            Cells:
            [
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)),
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)),
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))
            ]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    new WorldPosition(0, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null),
                new MapTile(
                    new WorldPosition(1, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null),
                new MapTile(
                    new WorldPosition(2, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        IReadOnlyList<MapObject> objects =
        [
            new MapTileFeature(new WorldPosition(0, 0), Depth: 0, new AssetSpriteKey.Tree(WorldMapTerrain.Human, 0))
        ];

        WorldRenderState state = new(
            Terrain: terrain,
            FogTiles: [new FogTile(new WorldPosition(2, 0))],
            MapObjects: objects,
            Stacks: [],
            Minimap: new MinimapState(1, 1, [], []),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        WorldRenderer renderer = new();
        SKImageInfo info = new(256, 256, SKColorType.RgbaF32, SKAlphaType.Premul);

        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(64, 64);
        renderer.Render(surface.Canvas, new WorldRenderContentProvider().Get(state, assets), state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        Assert.That(ReadColors(surface, [new SKPointI(64, 40)]), Is.EqualTo([SKColors.Red]));
    }

    [Test]
    public void OversizedObjectDirectSpriteTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Animation landmarkAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 2, Width: 400, Height: 400, OffsetX: -2, OffsetY: -20, TriggerFrame: null, SecondTriggerFrame: null),
            [
                builder.CreateSprite(SKColors.Blue, 400, 400, offsetX: -2, offsetY: -20),
                builder.CreateSprite(SKColors.Lime, 400, 400, offsetX: -2, offsetY: -20)
            ]);

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => groundSprite)
            .ResolveAnimation(_ => landmarkAnimation)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        WorldPosition position = new(0, 0);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    position,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Landmark landmark = new(
            position,
            Depth: 7,
            new WorldFootprint(position, 1, 1),
            new AssetAnimationKey.Landmark(new WorldLandmarkDefinitionId("lmark1")));

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [landmark], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(128, 128, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Translate(64, 64);

        IReadOnlyList<SKPointI> samplePoints = [new(63, 45)];

        surface.Canvas.Clear(SKColors.Transparent);
        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);
        IReadOnlyList<SKColor> firstFrame = ReadColors(surface, samplePoints);

        surface.Canvas.Clear(SKColors.Transparent);
        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.FromSeconds(0.1), TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);
        IReadOnlyList<SKColor> secondFrame = ReadColors(surface, samplePoints);

        IReadOnlyList<DirectSprite> expectedSprites =
        [
            new DirectSprite.Animation(landmark.AssetKey, new Point(198, 380), landmark.Depth, new Rect(-2, -20, 400, 400), Opacity: 1.0)
        ];

        Assert.That(content.ObjectAtlas.SpriteCount, Is.Zero);
        Assert.That(content.ObjectSpriteDepths, Is.Empty);
        Assert.That(content.DirectSprites, Is.EqualTo(expectedSprites));
        Assert.That(firstFrame, Is.EqualTo([SKColors.Blue]));
        Assert.That(secondFrame, Is.EqualTo([SKColors.Lime]));
    }

    [Test]
    public void OversizedObjectDepthTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite treeSprite = builder.CreateSprite(SKColors.Red, 64, 64, offsetX: -32, offsetY: -32);
        Animation landmarkAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 2, Width: 400, Height: 400, OffsetX: -2, OffsetY: -20, TriggerFrame: null, SecondTriggerFrame: null),
            [
                builder.CreateSprite(SKColors.Blue, 400, 400, offsetX: -2, offsetY: -20),
                builder.CreateSprite(SKColors.Lime, 400, 400, offsetX: -2, offsetY: -20)
            ]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? groundSprite : treeSprite)
            .ResolveAnimation(_ => landmarkAnimation)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        WorldPosition position = new(0, 0);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    position,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        MapTileFeature treeBelow = new(position, Depth: 3, new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0));
        MapTileFeature treeAbove = new(position, Depth: 9, new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0));
        Landmark landmark = new(
            position,
            Depth: 6,
            new WorldFootprint(position, 1, 1),
            new AssetAnimationKey.Landmark(new WorldLandmarkDefinitionId("lmark1")));

        Assert.That(RenderObjectsOverTile(assets, terrain, [treeBelow, landmark]), Is.EqualTo(SKColors.Blue));
        Assert.That(RenderObjectsOverTile(assets, terrain, [landmark, treeAbove]), Is.EqualTo(SKColors.Red));
    }

    [Test]
    public void StackDestroyTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Animation deathAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 3, Width: 8, Height: 8, OffsetX: -4, OffsetY: -8, TriggerFrame: null, SecondTriggerFrame: null),
            [
                builder.CreateSprite(SKColors.Blue, 8, 8, offsetX: -4, offsetY: -8),
                builder.CreateSprite(SKColors.Lime, 8, 8, offsetX: -4, offsetY: -8),
                builder.CreateSprite(SKColors.Red, 8, 8, offsetX: -4, offsetY: -8)
            ]);

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => groundSprite)
            .ResolveAnimation(_ => deathAnimation)
            .ResolveSound(_ => null)
            .Build();

        WorldPosition position = new(0, 0);

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    position,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Effect stackDestroy = new(
            new AssetSoundKey.StackDestroy(),
            position,
            Depth: 6,
            new AssetAnimationKey.StackDestroyEffect(),
            FrameCount: 3);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: stackDestroy, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(128, 128, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Translate(64, 64);

        IReadOnlyList<SKPointI> samplePoints = [new(64, 60)];
        List<SKColor> frames = [];

        foreach (double seconds in new[] { 0.0, 0.07, 0.14, 0.21 })
        {
            surface.Canvas.Clear(SKColors.Transparent);
            renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.FromSeconds(seconds), renderResolution: 1.0f, hitboxOverlay: false);

            IReadOnlyList<SKColor> colors = ReadColors(surface, samplePoints);
            frames.Add(colors[0]);
        }

        Assert.That(stackDestroy.Duration, Is.EqualTo(TimeSpan.FromSeconds(3.0 / 15.0)));
        Assert.That(frames, Is.EqualTo([SKColors.Blue, SKColors.Lime, SKColors.Red, SKColors.Empty]));
    }

    [Test]
    public void StackCreateTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Animation createAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 3, Width: 8, Height: 8, OffsetX: -4, OffsetY: -8, TriggerFrame: null, SecondTriggerFrame: null),
            [
                builder.CreateSprite(SKColors.Blue, 8, 8, offsetX: -4, offsetY: -8),
                builder.CreateSprite(SKColors.Lime, 8, 8, offsetX: -4, offsetY: -8),
                builder.CreateSprite(SKColors.Red, 8, 8, offsetX: -4, offsetY: -8)
            ]);

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => groundSprite)
            .ResolveAnimation(_ => createAnimation)
            .ResolveSound(_ => null)
            .Build();

        WorldPosition position = new(0, 0);

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    position,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Effect stackCreate = new(
            new AssetSoundKey.StackCreate(),
            position,
            Depth: 6,
            new AssetAnimationKey.StackCreateEffect(),
            FrameCount: 3);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: stackCreate, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(128, 128, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Translate(64, 64);

        IReadOnlyList<SKPointI> samplePoints = [new(64, 60)];
        List<SKColor> frames = [];

        foreach (double seconds in new[] { 0.0, 0.07, 0.14, 0.21 })
        {
            surface.Canvas.Clear(SKColors.Transparent);
            renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.FromSeconds(seconds), renderResolution: 1.0f, hitboxOverlay: false);

            IReadOnlyList<SKColor> colors = ReadColors(surface, samplePoints);
            frames.Add(colors[0]);
        }

        Assert.That(stackCreate.Duration, Is.EqualTo(TimeSpan.FromSeconds(3.0 / 15.0)));
        Assert.That(frames, Is.EqualTo([SKColors.Blue, SKColors.Lime, SKColors.Red, SKColors.Empty]));
    }

    [Test]
    public void SummonSpellRevealTest()
    {
        WorldSpellDefinitionId spellId = new("G000SS0008");
        AssetAnimationKey.WorldSpellEffect overKey = new(spellId, AssetAnimationKey.WorldSpellEffectLayer.Over, Sublayer: 0);
        AssetAnimationKey.StackUnit summonKey = new(new UnitDefinitionId("summoned"), AssetAnimationKey.StackUnitPose.Stop, Direction: 0);

        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Animation effectAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 4, Width: 8, Height: 8, OffsetX: -20, OffsetY: -8, TriggerFrame: 2, SecondTriggerFrame: null),
            [
                builder.CreateSprite(SKColors.Blue, 8, 8, offsetX: -20, offsetY: -8),
                builder.CreateSprite(SKColors.Blue, 8, 8, offsetX: -20, offsetY: -8),
                builder.CreateSprite(SKColors.Blue, 8, 8, offsetX: -20, offsetY: -8),
                builder.CreateSprite(SKColors.Blue, 8, 8, offsetX: -20, offsetY: -8)
            ]);
        Animation summonAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 8, Height: 8, OffsetX: 12, OffsetY: -8, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Red, 8, 8, offsetX: 12, offsetY: -8)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => groundSprite)
            .ResolveAnimation(key => key.Equals(summonKey) ? summonAnimation : effectAnimation)
            .ResolveSound(_ => null)
            .Build();

        WorldPosition position = new(0, 0);

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    position,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        SpellCast spellCast = new(
            new AssetSoundKey.WorldSpellCast(spellId),
            position,
            [
                new SpellCastLayer.Summon(new Stack(position, Depth: 4, summonKey, ShadowKey: null, AuraKey: null, new AssetSpriteKey.StackBanner(0), Opacity: 1.0), RevealFrame: 2),
                new SpellCastLayer.Effect(overKey, Depth: 100, FrameCount: 4)
            ],
            new WorldStackId("summoned"),
            FrameCount: 4);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: spellCast, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(128, 128, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Translate(64, 64);

        IReadOnlyList<SKPointI> samplePoints = [new(48, 62), new(80, 62)];
        List<SKColor> effectSamples = [];
        List<SKColor> summonSamples = [];

        foreach (double seconds in new[] { 0.0, 0.07, 0.14, 0.21, 0.28 })
        {
            surface.Canvas.Clear(SKColors.Transparent);
            renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.FromSeconds(seconds), renderResolution: 1.0f, hitboxOverlay: false);

            IReadOnlyList<SKColor> colors = ReadColors(surface, samplePoints);
            effectSamples.Add(colors[0]);
            summonSamples.Add(colors[1]);
        }

        Assert.That(spellCast.Duration, Is.EqualTo(TimeSpan.FromSeconds(4.0 / 15.0)));
        Assert.That(effectSamples, Is.EqualTo([SKColors.Blue, SKColors.Blue, SKColors.Blue, SKColors.Blue, SKColors.Empty]));
        Assert.That(summonSamples, Is.EqualTo([SKColors.Empty, SKColors.Empty, SKColors.Red, SKColors.Red, SKColors.Red]));
    }

    [Test]
    public void SpellCastLayersTest()
    {
        WorldSpellDefinitionId spellId = new("G000SS0008");
        AssetAnimationKey.WorldSpellEffect underKey = new(spellId, AssetAnimationKey.WorldSpellEffectLayer.Under, Sublayer: 0);
        AssetAnimationKey.WorldSpellEffect overKey = new(spellId, AssetAnimationKey.WorldSpellEffectLayer.Over, Sublayer: 0);

        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite bagSprite = builder.CreateSprite(SKColors.Red, 16, 16, offsetX: -8, offsetY: -8);
        Animation underAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 16, Height: 16, OffsetX: -8, OffsetY: -8, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 16, 16, offsetX: -8, offsetY: -8)]);
        Animation overAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 16, Height: 16, OffsetX: -8, OffsetY: -8, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Lime, 16, 16, offsetX: -8, offsetY: -8)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key switch
            {
                AssetSpriteKey.Bag => bagSprite,
                _ => groundSprite
            })
            .ResolveAnimation(key => key.Equals(underKey) ? underAnimation : overAnimation)
            .ResolveSound(_ => null)
            .Build();

        WorldPosition position = new(0, 0);

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    position,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Bag bag = new(position, Depth: 5, new AssetSpriteKey.Bag(IsWater: false, 0));

        SpellCast underOnly = new(
            new AssetSoundKey.WorldSpellCast(spellId),
            position,
            [new SpellCastLayer.Effect(underKey, Depth: 1, FrameCount: 1)],
            SummonedStackId: null,
            FrameCount: 1);

        SpellCast overOnly = underOnly with
        {
            Layers = [new SpellCastLayer.Effect(overKey, Depth: 100, FrameCount: 1)]
        };

        Assert.That(RenderSpellOverBag(assets, terrain, bag, underOnly), Is.EqualTo(SKColors.Red));
        Assert.That(RenderSpellOverBag(assets, terrain, bag, overOnly), Is.EqualTo(SKColors.Lime));
    }

    [Test]
    public void StackEffectDepthTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite bagSprite = builder.CreateSprite(SKColors.Red, 16, 16, offsetX: -8, offsetY: -8);
        Animation effectAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 16, Height: 16, OffsetX: -8, OffsetY: -8, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 16, 16, offsetX: -8, offsetY: -8)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key switch
            {
                AssetSpriteKey.Bag => bagSprite,
                _ => groundSprite
            })
            .ResolveAnimation(_ => effectAnimation)
            .ResolveSound(_ => null)
            .Build();

        WorldPosition position = new(0, 0);

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    position,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Effect stackCreate = new(
            new AssetSoundKey.StackCreate(),
            position,
            Depth: 131648,
            new AssetAnimationKey.StackCreateEffect(),
            FrameCount: 1);

        Effect stackDestroy = new(
            new AssetSoundKey.StackDestroy(),
            position,
            Depth: 131647,
            new AssetAnimationKey.StackDestroyEffect(),
            FrameCount: 1);

        int belowEffects = 393;
        int aboveEffects = 131942;

        Assert.That(RenderEffectOverBag(assets, terrain, stackCreate, belowEffects), Is.EqualTo(SKColors.Blue));
        Assert.That(RenderEffectOverBag(assets, terrain, stackCreate, aboveEffects), Is.EqualTo(SKColors.Red));
        Assert.That(RenderEffectOverBag(assets, terrain, stackDestroy, belowEffects), Is.EqualTo(SKColors.Blue));
        Assert.That(RenderEffectOverBag(assets, terrain, stackDestroy, aboveEffects), Is.EqualTo(SKColors.Red));
    }

    [Test]
    public void SpellCastLayersAcrossTilesTest()
    {
        WorldSpellDefinitionId spellId = new("G000SS0008");
        AssetAnimationKey.WorldSpellEffect frontKey = new(spellId, AssetAnimationKey.WorldSpellEffectLayer.Front, Sublayer: 0);
        AssetAnimationKey.WorldSpellEffect topKey = new(spellId, AssetAnimationKey.WorldSpellEffectLayer.Top, Sublayer: 0);

        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite bagSprite = builder.CreateSprite(SKColors.Red, 64, 64, offsetX: -56, offsetY: -56);
        Animation frontAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 16, Height: 16, OffsetX: -8, OffsetY: -8, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 16, 16, offsetX: -8, offsetY: -8)]);
        Animation topAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 16, Height: 16, OffsetX: -8, OffsetY: -8, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Lime, 16, 16, offsetX: -8, offsetY: -8)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key switch
            {
                AssetSpriteKey.Bag => bagSprite,
                _ => groundSprite
            })
            .ResolveAnimation(key => key.Equals(frontKey) ? frontAnimation : topAnimation)
            .ResolveSound(_ => null)
            .Build();

        WorldPosition position = new(0, 0);
        WorldPosition inFront = new(1, 0);

        WorldMapCell cell = new(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0));

        WorldMap map = new(Width: 2, Height: 1, Cells: [cell, cell]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(position, new MapTileSurface.Ground(WorldMapTerrain.Human), RoadKey: null),
                new MapTile(inFront, new MapTileSurface.Ground(WorldMapTerrain.Human), RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Bag bag = new(inFront, Depth: 905, new AssetSpriteKey.Bag(IsWater: false, 0));

        SpellCast frontOnly = new(
            new AssetSoundKey.WorldSpellCast(spellId),
            position,
            [new SpellCastLayer.Effect(frontKey, Depth: 488, FrameCount: 1)],
            SummonedStackId: null,
            FrameCount: 1);

        SpellCast topOnly = frontOnly with
        {
            Layers = [new SpellCastLayer.Effect(topKey, Depth: 394754, FrameCount: 1)]
        };

        Assert.That(RenderSpellOverBag(assets, terrain, bag, frontOnly), Is.EqualTo(SKColors.Red));
        Assert.That(RenderSpellOverBag(assets, terrain, bag, topOnly), Is.EqualTo(SKColors.Lime));
    }

    [Test]
    public void MountainRemoveTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Animation dustAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 3, Width: 8, Height: 8, OffsetX: -4, OffsetY: -8, TriggerFrame: null, SecondTriggerFrame: null),
            [
                builder.CreateSprite(SKColors.Blue, 8, 8, offsetX: -4, offsetY: -8),
                builder.CreateSprite(SKColors.Lime, 8, 8, offsetX: -4, offsetY: -8),
                builder.CreateSprite(SKColors.Red, 8, 8, offsetX: -4, offsetY: -8)
            ]);

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => groundSprite)
            .ResolveAnimation(_ => dustAnimation)
            .ResolveSound(_ => null)
            .Build();

        WorldPosition position = new(0, 0);

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    position,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Effect mountainRemove = new(
            SoundKey: null,
            position,
            Depth: 6,
            new AssetAnimationKey.MountainRemoveEffect(5),
            FrameCount: 3);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: mountainRemove, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(128, 128, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Translate(64, 64);

        IReadOnlyList<SKPointI> samplePoints = [new(64, 60)];
        List<SKColor> frames = [];

        foreach (double seconds in new[] { 0.0, 0.07, 0.14, 0.21 })
        {
            surface.Canvas.Clear(SKColors.Transparent);
            renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.FromSeconds(seconds), renderResolution: 1.0f, hitboxOverlay: false);

            IReadOnlyList<SKColor> colors = ReadColors(surface, samplePoints);
            frames.Add(colors[0]);
        }

        Assert.That(mountainRemove.Duration, Is.EqualTo(TimeSpan.FromSeconds(3.0 / 15.0)));
        Assert.That(frames, Is.EqualTo([SKColors.Blue, SKColors.Lime, SKColors.Red, SKColors.Empty]));
    }

    [Test]
    public void TranslucentStackTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite bagSprite = builder.CreateSprite(SKColors.Red, 128, 128, offsetX: -64, offsetY: -64);
        Sprite bannerSprite = builder.CreateSprite(SKColors.Transparent, 1, 1);
        Animation body = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 4, 4)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key switch
            {
                AssetSpriteKey.Ground => groundSprite,
                AssetSpriteKey.StackBanner => bannerSprite,
                _ => bagSprite
            })
            .ResolveAnimation(_ => body)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    new WorldPosition(0, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        UnitDefinitionId unitDefinitionId = new("g000uu5130");
        Bag bag = new(new WorldPosition(0, 0), Depth: 3, new AssetSpriteKey.Bag(IsWater: false, 0));
        Stack stack = new(
            new WorldPosition(0, 0),
            Depth: 5,
            new AssetAnimationKey.StackUnit(unitDefinitionId, AssetAnimationKey.StackUnitPose.Stop, 0),
            ShadowKey: null,
            AuraKey: null,
            new AssetSpriteKey.StackBanner(4),
            Opacity: 0.5);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [bag], Stacks: [stack], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(256, 256, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(64, 64);

        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        Assert.That(ReadColors(surface, [new SKPointI(65, 65)]), Is.EqualTo([new SKColor(127, 0, 128, 255)]));
    }

    [Test]
    public void TranslucentMovingStackTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite bagSprite = builder.CreateSprite(SKColors.Red, 128, 128, offsetX: -64, offsetY: -64);
        Sprite bannerSprite = builder.CreateSprite(SKColors.Transparent, 1, 1);
        Animation body = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Blue, 4, 4)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key switch
            {
                AssetSpriteKey.Ground => groundSprite,
                AssetSpriteKey.StackBanner => bannerSprite,
                _ => bagSprite
            })
            .ResolveAnimation(_ => body)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 2,
            Height: 1,
            Cells:
            [
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)),
                new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))
            ]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    new WorldPosition(0, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null),
                new MapTile(
                    new WorldPosition(1, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        UnitDefinitionId unitDefinitionId = new("g000uu5130");
        StackMove stackMove = new(
            new WorldStackId("Stack1"),
            new AssetSoundKey.StackUnitMove(unitDefinitionId, Variant: 0),
            new AssetSpriteKey.StackBanner(4),
            [
                new StackMoveStep(
                    new WorldPosition(0, 0),
                    3,
                    new AssetAnimationKey.StackUnit(unitDefinitionId, AssetAnimationKey.StackUnitPose.Move, 0),
                    ShadowKey: null,
                    AuraKey: null,
                    TimeSpan.Zero),
                new StackMoveStep(
                    new WorldPosition(1, 0),
                    4,
                    new AssetAnimationKey.StackUnit(unitDefinitionId, AssetAnimationKey.StackUnitPose.Move, 0),
                    ShadowKey: null,
                    AuraKey: null,
                    TimeSpan.FromMilliseconds(200))
            ],
            Opacity: 0.5,
            IsObjective: false,
            Highlight: null);

        Assert.That(RenderMoveOverBag(assets, terrain, stackMove, bagDepth: 3), Is.EqualTo(new SKColor(127, 0, 128, 255)));
    }

    [Test]
    public void CursorBehindObjectTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Animation landmarkAnimation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 128, Height: 128, OffsetX: -64, OffsetY: -64, TriggerFrame: null, SecondTriggerFrame: null),
            [builder.CreateSprite(SKColors.Red, 128, 128, offsetX: -64, offsetY: -64)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => groundSprite)
            .ResolveAnimation(_ => landmarkAnimation)
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        WorldPosition position = new(0, 0);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    position,
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Landmark landmark = new(
            position,
            Depth: 1,
            new WorldFootprint(position, 1, 1),
            new AssetAnimationKey.Landmark(new WorldLandmarkDefinitionId("lmark1")));

        Cursor cursor = new(position, Depth: 0, Theme.World.HoverCursorInnerColor, Theme.World.HoverCursorOuterColor, Scale: 1.0);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [landmark], Stacks: [], Minimap: new MinimapState(1, 1, [], []), Cursors: [cursor], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(128, 128, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Translate(64, 64);
        surface.Canvas.Clear(SKColors.Transparent);
        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        Assert.That(ReadColors(surface, [new SKPointI(83, 64), new SKPointI(10, 10)]), Is.EqualTo([new SKColor(250, 68, 40, 255), SKColors.Red]));
    }

    [Test]
    public void StackBehindStackTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite groundSprite = builder.CreateSprite(SKColors.Transparent, 64, 32);
        Sprite bannerSprite = builder.CreateSprite(SKColors.Transparent, 1, 1);
        AssetAnimationMetadata metadata = new(FrameCount: 1, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null);
        Animation rearBody = builder.CreateAnimation(metadata, [builder.CreateSprite(SKColors.Red, 4, 4)]);
        Animation frontBody = builder.CreateAnimation(metadata, [builder.CreateSprite(SKColors.Blue, 4, 4)]);

        using TestAssetProvider assets = builder
            .ResolveSprite(key => key switch
            {
                AssetSpriteKey.StackBanner => bannerSprite,
                _ => groundSprite
            })
            .ResolveAnimation(key => key switch
            {
                AssetAnimationKey.StackBoat { Race: WorldRace.Elf } => rearBody,
                _ => frontBody
            })
            .ResolveSound(_ => null)
            .Build();

        WorldMap map = new(
            Width: 1,
            Height: 1,
            Cells: [new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0))]);

        MapTerrain terrain = new(
            map,
            [
                new MapTile(
                    new WorldPosition(0, 0),
                    new MapTileSurface.Ground(WorldMapTerrain.Human),
                    RoadKey: null)
            ], [WorldMapTerrain.Human], []);

        Stack rear = new(new WorldPosition(0, 0), 3, new AssetAnimationKey.StackBoat(WorldRace.Elf, AssetAnimationKey.StackUnitPose.Stop, 0), ShadowKey: null, AuraKey: null, new AssetSpriteKey.StackBanner(4), Opacity: 1.0);
        Stack front = new(new WorldPosition(0, 0), 5, new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Stop, 0), ShadowKey: null, AuraKey: null, new AssetSpriteKey.StackBanner(4), Opacity: 1.0);

        WorldRenderState state = new(Terrain: terrain, FogTiles: [], ObjectiveMarkers: [], AmbientSounds: [], MapObjects: [], Stacks: [rear, front], Minimap: new MinimapState(1, 1, [], []), Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), SelectedPath: null, Animation: null, Clouds: []);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();
        WorldRenderContent content = provider.Get(state, assets);

        SKImageInfo info = new(256, 256, SKColorType.RgbaF32, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(64, 64);

        renderer.Render(surface.Canvas, content, state, assets, TimeSpan.Zero, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        Assert.That(ReadColors(surface, [new SKPointI(65, 65)]), Is.EqualTo([SKColors.Blue]));
    }
}
