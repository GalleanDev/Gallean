namespace Umbra;

internal sealed record SurfaceOptions
{
    public double Background { get; init; } = 128.0 / 255.0;
    public int MaxPixels { get; init; } = 20 * 1024 * 1024;
}
