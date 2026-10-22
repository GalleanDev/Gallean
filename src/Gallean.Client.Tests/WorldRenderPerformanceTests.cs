using System.Collections.Immutable;
using Avalonia;
using Avalonia.Skia;
using Gallean.Tests.Common;
using SkiaSharp;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client.Tests;

[Explicit]
[NonParallelizable]
public sealed class WorldRenderPerformanceTests
{
    private const int ViewportWidth = 3840;
    private const int ViewportHeight = 2160;
    private const double MinimumZoom = 0.55;

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

    private static PerformanceMeasurement MeasureCold(
        SKCanvas canvas,
        WorldRenderContentProvider provider,
        WorldRenderer renderer,
        WorldRenderState state,
        IAssetProvider assets,
        TimeSpan animationTime)
    {
        PerformanceTimer timer = new();
        WorldRenderContent content = provider.Get(state, assets);
        renderer.Render(canvas, content, state, assets, animationTime, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);
        canvas.Flush();

        return timer.Stop(iterations: 1);
    }

    private static PerformanceMeasurement MeasureHot(
        SKCanvas canvas,
        WorldRenderContentProvider provider,
        WorldRenderer renderer,
        WorldRenderState state,
        IAssetProvider assets,
        TimeSpan animationTime)
    {
        const int warmup = 5;
        const int iterations = 60;

        for (int index = 0; index < warmup; index++)
        {
            renderer.Render(canvas, provider.Get(state, assets), state, assets, animationTime, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);
        }

        canvas.Flush();

        PerformanceTimer timer = new();

        for (int index = 0; index < iterations; index++)
        {
            renderer.Render(canvas, provider.Get(state, assets), state, assets, animationTime, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);
        }

        canvas.Flush();

        return timer.Stop(iterations);
    }

    private static PerformanceMeasurement MeasureRecord(
        WorldRenderContentProvider provider,
        WorldRenderer renderer,
        WorldRenderState state,
        IAssetProvider assets,
        TimeSpan animationTime,
        SKMatrix view)
    {
        const int warmup = 5;
        const int iterations = 60;

        for (int index = 0; index < warmup; index++)
        {
            RecordFrame(provider, renderer, state, assets, animationTime, view);
        }

        PerformanceTimer timer = new();

        for (int index = 0; index < iterations; index++)
        {
            RecordFrame(provider, renderer, state, assets, animationTime, view);
        }

        return timer.Stop(iterations);
    }

    private static void RecordFrame(
        WorldRenderContentProvider provider,
        WorldRenderer renderer,
        WorldRenderState state,
        IAssetProvider assets,
        TimeSpan animationTime,
        SKMatrix view)
    {
        using SKPictureRecorder recorder = new();

        SKCanvas canvas = recorder.BeginRecording(new SKRect(0, 0, ViewportWidth, ViewportHeight));
        canvas.SetMatrix(view);

        renderer.Render(canvas, provider.Get(state, assets), state, assets, animationTime, TimeSpan.Zero, renderResolution: 1.0f, hitboxOverlay: false);

        using SKPicture picture = recorder.EndRecording();
    }

    private static SKMatrix BuildViewMatrix(Point center, double zoom)
    {
        Matrix view = Matrix.CreateTranslation(-center.X, -center.Y) *
                      Matrix.CreateScale(zoom, zoom) *
                      Matrix.CreateTranslation(ViewportWidth / 2.0, ViewportHeight / 2.0);

        return view.ToSKMatrix();
    }

    private static Rect ComputeWorldBounds(WorldRenderState state)
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (MapTile tile in state.Terrain.MapTiles)
        {
            Point center = WorldTransform.ToWorldPoint(tile.Position);
            minX = Math.Min(minX, center.X);
            minY = Math.Min(minY, center.Y);
            maxX = Math.Max(maxX, center.X);
            maxY = Math.Max(maxY, center.Y);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private static WorldRenderState BuildRenderState(int mapSide, double waterFraction)
    {
        List<MapTile> tiles = new(mapSide * mapSide);
        List<FogTile> fogTiles = new(mapSide * mapSide);
        ImmutableArray<WorldMapCell>.Builder cells = ImmutableArray.CreateBuilder<WorldMapCell>(mapSide * mapSide);

        int waterStride;
        if (waterFraction > 0.0)
        {
            waterStride = Math.Max(2, (int)Math.Round(1.0 / waterFraction));
        }
        else
        {
            waterStride = int.MaxValue;
        }

        for (int y = 0; y < mapSide; y++)
        {
            for (int x = 0; x < mapSide; x++)
            {
                WorldPosition position = new(x, y);
                bool isWater = waterStride != int.MaxValue && ((x + y * 3) % waterStride == 0);

                MapTileSurface surface;

                if (isWater)
                {
                    surface = new MapTileSurface.Water();
                    cells.Add(new WorldMapCell(WorldMapTerrain.Water, WorldMapFeature.None, new WorldObjectVariant(0)));
                }
                else
                {
                    surface = new MapTileSurface.Ground(WorldMapTerrain.Human);
                    cells.Add(new WorldMapCell(WorldMapTerrain.Human, WorldMapFeature.None, new WorldObjectVariant(0)));
                }

                tiles.Add(new MapTile(position, surface, RoadKey: null));
                fogTiles.Add(new FogTile(position));
            }
        }

        WorldMap map = new(mapSide, mapSide, cells.MoveToImmutable());

        return new WorldRenderState(Terrain: new MapTerrain(map, tiles, [WorldMapTerrain.Human], []), MapObjects: [], Stacks: [], Animation: null, Clouds: [], FogTiles: fogTiles, ObjectiveMarkers: [], AmbientSounds: [], SelectedPath: null, Cursors: [], Reach: null, Highlights: new HighlightOverlay([]), Minimap: new MinimapState(mapSide, mapSide, [], []));
    }

    private static WorldRenderState AddMapObjects(WorldRenderState state)
    {
        List<MapObject> objects = [];

        foreach (MapTile tile in state.Terrain.MapTiles)
        {
            if (tile.Surface is MapTileSurface.Ground)
            {
                objects.Add(new MapTileFeature(
                    tile.Position,
                    tile.Position.X + tile.Position.Y,
                    new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0)));
            }
        }

        return state with { MapObjects = [.. objects.OrderBy(item => item.Depth)] };
    }

    [TestCase(120, 0.25, MinimumZoom)]
    [TestCase(120, 0.25, 1.0)]
    [TestCase(120, 0.25, 2.0)]
    [TestCase(120, 0.0, 1.0)]
    public void RecordMapTest(int mapSide, double waterFraction, double zoom)
    {
        using TestAssetProvider assets = CreateAssets();
        WorldRenderState state = BuildRenderState(mapSide, waterFraction);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();

        Rect worldBounds = ComputeWorldBounds(state);
        SKMatrix view = BuildViewMatrix(worldBounds.Center, zoom);

        PerformanceMeasurement measurement = MeasureRecord(provider, renderer, state, assets, TimeSpan.FromSeconds(1.3), view);

        TestContext.Out.WriteLine($"Record: Time = {measurement.ElapsedTime.TotalMilliseconds:F3} ms, CPU = {measurement.CpuTime.TotalMilliseconds:F3} ms");
    }

    [TestCase(120, MinimumZoom)]
    [TestCase(120, 2.0)]
    public void RecordObjectsTest(int mapSide, double zoom)
    {
        using TestAssetProvider assets = CreateAssets();
        WorldRenderState state = AddMapObjects(BuildRenderState(mapSide, waterFraction: 0.0));
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();

        Rect worldBounds = ComputeWorldBounds(state);
        SKMatrix view = BuildViewMatrix(worldBounds.Center, zoom);

        PerformanceMeasurement measurement = MeasureRecord(provider, renderer, state, assets, TimeSpan.FromSeconds(1.3), view);

        TestContext.Out.WriteLine($"Objects: {state.MapObjects.Count:N0}");
        TestContext.Out.WriteLine($"Record: Time = {measurement.ElapsedTime.TotalMilliseconds:F3} ms, CPU = {measurement.CpuTime.TotalMilliseconds:F3} ms");
    }

    [TestCase(72, 0.25)]
    [TestCase(96, 0.25)]
    [TestCase(96, 0.0)]
    [TestCase(120, 0.25)]
    [TestCase(120, 0.0)]
    public void RenderMapTest(int mapSide, double waterFraction)
    {
        using TestAssetProvider assets = CreateAssets();
        WorldRenderState state = BuildRenderState(mapSide, waterFraction);
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();

        SKImageInfo info = new(ViewportWidth, ViewportHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);

        Rect worldBounds = ComputeWorldBounds(state);
        Point center = worldBounds.Center;
        SKMatrix view = BuildViewMatrix(center, MinimumZoom);

        TimeSpan animationTime = TimeSpan.FromSeconds(1.3);

        SKCanvas canvas = surface.Canvas;
        canvas.Save();
        canvas.SetMatrix(view);

        PerformanceMeasurement cold = MeasureCold(canvas, provider, renderer, state, assets, animationTime);
        PerformanceMeasurement hot = MeasureHot(canvas, provider, renderer, state, assets, animationTime);

        canvas.Restore();

        TestContext.Out.WriteLine($"Cold: Time = {Math.Round(cold.ElapsedTime.TotalMilliseconds):N0} ms, CPU = {Math.Round(cold.CpuTime.TotalMilliseconds):N0} ms");
        TestContext.Out.WriteLine($"Hot: Time = {Math.Round(hot.ElapsedTime.TotalMilliseconds):N0} ms, CPU = {Math.Round(hot.CpuTime.TotalMilliseconds):N0} ms, Allocated={hot.AllocatedBytes / 1024} KB");
    }

    [TestCase(72)]
    [TestCase(120)]
    public void RenderObjectsTest(int mapSide)
    {
        using TestAssetProvider assets = CreateAssets();
        WorldRenderState state = AddMapObjects(BuildRenderState(mapSide, waterFraction: 0.0));
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();

        SKImageInfo info = new(ViewportWidth, ViewportHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);

        Rect worldBounds = ComputeWorldBounds(state);
        Point center = worldBounds.Center;
        SKMatrix view = BuildViewMatrix(center, MinimumZoom);
        TimeSpan animationTime = TimeSpan.FromSeconds(1.3);

        SKCanvas canvas = surface.Canvas;
        canvas.Save();
        canvas.SetMatrix(view);

        PerformanceMeasurement cold = MeasureCold(canvas, provider, renderer, state, assets, animationTime);
        PerformanceMeasurement hot = MeasureHot(canvas, provider, renderer, state, assets, animationTime);

        canvas.Restore();

        TestContext.Out.WriteLine($"Objects: {state.MapObjects.Count:N0}");
        TestContext.Out.WriteLine($"Cold: Time = {Math.Round(cold.ElapsedTime.TotalMilliseconds):N0} ms, CPU = {Math.Round(cold.CpuTime.TotalMilliseconds):N0} ms");
        TestContext.Out.WriteLine($"Hot: Time = {Math.Round(hot.ElapsedTime.TotalMilliseconds):N0} ms, CPU = {Math.Round(hot.CpuTime.TotalMilliseconds):N0} ms, Allocated={hot.AllocatedBytes / 1024} KB");
    }

    [TestCase(120)]
    public void RenderObjectsZoomedTest(int mapSide)
    {
        const double zoom = 2.0;

        using TestAssetProvider assets = CreateAssets();
        WorldRenderState state = AddMapObjects(BuildRenderState(mapSide, waterFraction: 0.0));
        WorldRenderContentProvider provider = new();
        WorldRenderer renderer = new();

        SKImageInfo info = new(ViewportWidth, ViewportHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);

        Rect worldBounds = ComputeWorldBounds(state);
        Point center = worldBounds.Center;
        SKMatrix view = BuildViewMatrix(center, zoom);
        TimeSpan animationTime = TimeSpan.FromSeconds(1.3);

        SKCanvas canvas = surface.Canvas;
        canvas.Save();
        canvas.SetMatrix(view);

        PerformanceMeasurement cold = MeasureCold(canvas, provider, renderer, state, assets, animationTime);
        PerformanceMeasurement hot = MeasureHot(canvas, provider, renderer, state, assets, animationTime);

        canvas.Restore();

        TestContext.Out.WriteLine($"Objects: {state.MapObjects.Count:N0}");
        TestContext.Out.WriteLine($"Cold: Time = {Math.Round(cold.ElapsedTime.TotalMilliseconds):N0} ms, CPU = {Math.Round(cold.CpuTime.TotalMilliseconds):N0} ms");
        TestContext.Out.WriteLine($"Hot: Time = {Math.Round(hot.ElapsedTime.TotalMilliseconds):N0} ms, CPU = {Math.Round(hot.CpuTime.TotalMilliseconds):N0} ms, Allocated={hot.AllocatedBytes / 1024} KB");
    }
}
