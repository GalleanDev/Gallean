using Avalonia;
using Avalonia.Skia;
using Gallean.Common;
using Gallean.Tests.Common;
using SkiaSharp;

namespace Gallean.Client.Tests;

public sealed class WaterTintCalibrationTests
{
    private const int MapSize = 32;
    private const int ChannelLeft = 14;
    private const int ChannelWidth = 3;
    private const int SampleFrame = 0;
    private const int ColorTolerance = 2;
    private const int TileSampleWidth = WorldTransform.TileHalfWidth / 2;
    private const int TileSampleHeight = WorldTransform.TileHalfHeight / 2;

    private static readonly TimeSpan SampleTime = TimeSpan.FromSeconds(3);

    private static bool CompareColor(SKColor left, SKColor right)
    {
        return Math.Abs(left.Red - right.Red) <= ColorTolerance &&
               Math.Abs(left.Green - right.Green) <= ColorTolerance &&
               Math.Abs(left.Blue - right.Blue) <= ColorTolerance;
    }

    private static WorldRenderContentProvider.WaterTiles BuildRiver()
    {
        List<WorldPosition> tiles = [];

        for (int y = 0; y < MapSize; y++)
        {
            for (int x = ChannelLeft; x < ChannelLeft + ChannelWidth; x++)
            {
                tiles.Add(new WorldPosition(x, y));
            }
        }

        return new WorldRenderContentProvider.WaterTiles(MapSize, MapSize, tiles);
    }

    private static SKColor MeasureMeanColor(IReadOnlyList<SKImage> images)
    {
        long red = 0;
        long green = 0;
        long blue = 0;
        long count = 0;

        foreach (SKImage image in images)
        {
            SKImageInfo info = new(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);

            using SKBitmap bitmap = new(info);

            image.ReadPixels(bitmap.PeekPixels(), 0, 0);

            byte[] pixels = bitmap.Bytes;

            for (int offset = 0; offset < pixels.Length; offset += 4)
            {
                if (pixels[offset + 3] > 0)
                {
                    blue += pixels[offset];
                    green += pixels[offset + 1];
                    red += pixels[offset + 2];
                    count++;
                }
            }
        }

        Assert.That(count, Is.GreaterThan(0));

        return new SKColor((byte)(red / count), (byte)(green / count), (byte)(blue / count));
    }

    private static SKColor MeasureAlbedoColor(IAssetProvider assets)
    {
        using CleanupList leases = new();

        AssetCache<Animation>.Lease lease = leases.Add(assets.GetRequiredAnimation(new AssetAnimationKey.Water()));
        Animation water = lease.Value;
        Sprite frame = water.FrameByIndex(SampleFrame, looping: true);

        return MeasureMeanColor([frame.Image]);
    }

    private static SKImage RenderTile(WorldRenderContent.WaterLayer layer, WorldPosition tile)
    {
        Point center = WorldTransform.ToWorldPoint(tile.X + 0.5, tile.Y + 0.5);

        SKImageInfo info = new(TileSampleWidth, TileSampleHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

        using SKSurface surface = SKSurface.Create(info);
        SKCanvas canvas = surface.Canvas;

        canvas.Clear(Theme.World.BackdropColor.ToSKColor());
        canvas.Translate((TileSampleWidth / 2.0f) - (float)center.X, (TileSampleHeight / 2.0f) - (float)center.Y);

        WorldRenderContentProvider.Water.Draw(canvas, layer, SampleTime, zoom: 1.0, SampleFrame);

        return surface.CreateRasterImage();
    }

    private static SKColor MeasureShoreColor(IAssetProvider assets)
    {
        WorldRenderContentProvider.WaterTiles tiles = BuildRiver();
        WorldRenderContent.WaterLayer layer = Assert.NotNull(WorldRenderContentProvider.Water.Build(tiles, assets));

        using CleanupList samples = new();

        List<SKImage> images = [];

        foreach (WorldPosition tile in tiles.Tiles)
        {
            images.Add(samples.Add(RenderTile(layer, tile)));
        }

        return MeasureMeanColor(images);
    }

    [Test]
    public void AlbedoColorTest()
    {
        using AssetProvider provider = TestAssets.LoadOrIgnore();

        Assert.That(
            MeasureAlbedoColor(provider),
            Is.EqualTo(new SKColor(29, 73, 77)).Using<SKColor>(CompareColor));
    }

    [Test]
    public void ShoreColorTest()
    {
        using AssetProvider provider = TestAssets.LoadOrIgnore();

        Assert.That(
            MeasureShoreColor(provider),
            Is.EqualTo(new SKColor(37, 92, 98)).Using<SKColor>(CompareColor));
    }
}
