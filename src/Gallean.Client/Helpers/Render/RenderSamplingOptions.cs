using SkiaSharp;

namespace Gallean.Client;

internal static class RenderSamplingOptions
{
    public static SKSamplingOptions Nearest { get; } = new(SKFilterMode.Nearest, SKMipmapMode.None);
    public static SKSamplingOptions Linear { get; } = new(SKFilterMode.Linear, SKMipmapMode.None);
}
