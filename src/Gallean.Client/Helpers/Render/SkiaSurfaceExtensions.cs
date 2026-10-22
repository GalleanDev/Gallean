using SkiaSharp;

namespace Gallean.Client;

internal static class SkiaSurfaceExtensions
{
    public static SKImage CreateRasterImage(this SKSurface surface)
    {
        SKImage snapshot = surface.Snapshot();
        SKImage image = snapshot.ToRasterImage(ensurePixelData: true);

        SkiaMemoryPressure.TrackImage(image);

        return image;
    }
}
