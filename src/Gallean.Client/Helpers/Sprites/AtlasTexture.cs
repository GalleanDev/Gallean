using SkiaSharp;

namespace Gallean.Client;

internal sealed class AtlasTexture : IDisposable
{
    public readonly record struct RegionKey(SKImage Image, int Width, int Height);

    public delegate void DrawRegion(SKCanvas canvas, SKImage source, SKRect destination);

    private const int WidthMin = 256;
    private const int WidthMax = 8 * 1024;
    private const int WidthAlignment = 128;
    private const int RegionPadding = 1;

    private AtlasTexture(SKImage image, IReadOnlyDictionary<RegionKey, SKRect> regions)
    {
        Image = image;
        Regions = regions;
    }

    public SKImage Image { get; }
    public IReadOnlyDictionary<RegionKey, SKRect> Regions { get; }

    public void Dispose()
    {
        Image.Dispose();
    }

    public static AtlasTexture Build(IReadOnlySet<RegionKey> regions)
    {
        return Build(regions, DrawSource);
    }

    public static AtlasTexture Build(IReadOnlySet<RegionKey> regions, DrawRegion draw)
    {
        List<RegionKey> orderedRegions = regions
            .OrderByDescending(region => region.Height)
            .ThenByDescending(region => region.Width)
            .ToList();

        int atlasWidth = ChooseAtlasWidth(orderedRegions);
        int x = RegionPadding;
        int y = RegionPadding;
        int rowHeight = 0;
        Dictionary<RegionKey, SKRect> sources = [];

        foreach (RegionKey region in orderedRegions)
        {
            if (x + region.Width + RegionPadding > atlasWidth)
            {
                x = RegionPadding;
                y += rowHeight + RegionPadding;
                rowHeight = 0;
            }

            sources[region] = new SKRect(x, y, x + region.Width, y + region.Height);
            x += region.Width + RegionPadding;
            rowHeight = Math.Max(rowHeight, region.Height);
        }

        int atlasHeight = Math.Max(1, y + rowHeight + RegionPadding);
        SKImageInfo info = new(atlasWidth, atlasHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = CreateSurface(info);
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        foreach ((RegionKey region, SKRect destination) in sources)
        {
            draw(canvas, region.Image, destination);
        }

        return new AtlasTexture(surface.CreateRasterImage(), sources);
    }

    private static int ChooseAtlasWidth(IReadOnlyList<RegionKey> orderedRegions)
    {
        long area = 0;
        int maxRegionWidth = 0;

        foreach (RegionKey region in orderedRegions)
        {
            area += (long)(region.Width + RegionPadding) * (region.Height + RegionPadding);
            maxRegionWidth = Math.Max(maxRegionWidth, region.Width);
        }

        int ideal = (int)Math.Ceiling(Math.Sqrt(area));
        int width = Math.Max(maxRegionWidth + RegionPadding * 2, ideal);

        width = (width + WidthAlignment - 1) / WidthAlignment * WidthAlignment;
        width = Math.Clamp(width, WidthMin, WidthMax);

        return Math.Max(width, maxRegionWidth + RegionPadding * 2);
    }

    private static SKSurface CreateSurface(SKImageInfo info)
    {
        SKSurface surface = SKSurface.Create(info);

        if (surface is null)
        {
            throw new InvalidOperationException($"Failed to create a {info.Width}×{info.Height} atlas surface.");
        }

        return surface;
    }

    private static void DrawSource(SKCanvas canvas, SKImage source, SKRect destination)
    {
        canvas.DrawImage(source, destination, RenderSamplingOptions.Linear);
    }
}
