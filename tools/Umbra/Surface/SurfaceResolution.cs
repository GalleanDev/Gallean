namespace Umbra;

internal sealed record SurfaceResolution(int PixelHeight, int PixelWidth, int ContentHeight, int ContentWidth)
{
    private const int Alignment = 16;

    public static SurfaceResolution Calculate(int pixelHeight, int pixelWidth, int maxPixels)
    {
        double scale = Math.Min(1, Math.Sqrt(maxPixels / ((double)pixelHeight * pixelWidth)));

        int contentHeight = Math.Max(1, (int)Math.Round(pixelHeight * scale));
        int contentWidth = Math.Max(1, (int)Math.Round(pixelWidth * scale));

        return new SurfaceResolution(Align(contentHeight), Align(contentWidth), contentHeight, contentWidth);
    }

    private static int Align(int extent)
    {
        int aligned = (extent + Alignment - 1) / Alignment * Alignment;

        return Math.Max(Alignment, aligned);
    }
}
