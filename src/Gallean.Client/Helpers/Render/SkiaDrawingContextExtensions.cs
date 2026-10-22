using Avalonia.Media;
using Avalonia.Skia;

namespace Gallean.Client;

internal static class SkiaDrawingContextExtensions
{
    public static ISkiaSharpApiLease GetSkiaApiLease(this ImmediateDrawingContext context)
    {
        if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is ISkiaSharpApiLeaseFeature feature)
        {
            return feature.Lease();
        }
        else
        {
            throw new InvalidOperationException("Cannot use Skia API for a drawing context.");
        }
    }
}
