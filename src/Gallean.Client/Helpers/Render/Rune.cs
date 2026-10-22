using SkiaSharp;

namespace Gallean.Client;

internal sealed record Rune(IReadOnlyList<IReadOnlyList<SKPoint>> Strokes)
{
    public static Rune Fehu { get; } = new([
        [new SKPoint(-0.5f, -1.0f), new SKPoint(-0.5f, 1.0f)],
        [new SKPoint(-0.5f, -0.35f), new SKPoint(0.6f, -1.0f)],
        [new SKPoint(-0.5f, 0.2f), new SKPoint(0.6f, -0.45f)]
    ]);

    public static Rune Uruz { get; } = new([
        [new SKPoint(-0.6f, 1.0f), new SKPoint(-0.6f, -1.0f), new SKPoint(0.6f, -0.35f), new SKPoint(0.6f, 1.0f)]
    ]);

    public static Rune Thurisaz { get; } = new([
        [new SKPoint(-0.5f, -1.0f), new SKPoint(-0.5f, 1.0f)],
        [new SKPoint(-0.5f, -0.55f), new SKPoint(0.5f, 0.0f), new SKPoint(-0.5f, 0.55f)]
    ]);

    public static Rune Ansuz { get; } = new([
        [new SKPoint(-0.5f, -1.0f), new SKPoint(-0.5f, 1.0f)],
        [new SKPoint(-0.5f, -1.0f), new SKPoint(0.6f, -0.35f)],
        [new SKPoint(-0.5f, -0.45f), new SKPoint(0.6f, 0.2f)]
    ]);

    public static Rune Raido { get; } = new([
        [new SKPoint(-0.55f, -1.0f), new SKPoint(-0.55f, 1.0f)],
        [new SKPoint(-0.55f, -1.0f), new SKPoint(0.5f, -0.5f), new SKPoint(-0.55f, 0.0f), new SKPoint(0.6f, 1.0f)]
    ]);

    public static Rune Kaunan { get; } = new([
        [new SKPoint(0.45f, -1.0f), new SKPoint(-0.45f, -0.35f), new SKPoint(0.45f, 0.3f)]
    ]);

    public static Rune Gebo { get; } = new([
        [new SKPoint(-0.6f, -1.0f), new SKPoint(0.6f, 1.0f)],
        [new SKPoint(0.6f, -1.0f), new SKPoint(-0.6f, 1.0f)]
    ]);

    public static Rune Wunjo { get; } = new([
        [new SKPoint(-0.5f, -1.0f), new SKPoint(-0.5f, 1.0f)],
        [new SKPoint(-0.5f, -1.0f), new SKPoint(0.5f, -0.5f), new SKPoint(-0.5f, 0.0f)]
    ]);

    public static Rune Hagalaz { get; } = new([
        [new SKPoint(-0.6f, -1.0f), new SKPoint(-0.6f, 1.0f)],
        [new SKPoint(0.6f, -1.0f), new SKPoint(0.6f, 1.0f)],
        [new SKPoint(-0.6f, -0.45f), new SKPoint(0.6f, 0.25f)]
    ]);

    public static Rune Naudiz { get; } = new([
        [new SKPoint(0.0f, -1.0f), new SKPoint(0.0f, 1.0f)],
        [new SKPoint(-0.55f, -0.35f), new SKPoint(0.55f, 0.3f)]
    ]);

    public static Rune Isaz { get; } = new([
        [new SKPoint(0.0f, -1.0f), new SKPoint(0.0f, 1.0f)]
    ]);

    public static Rune Tiwaz { get; } = new([
        [new SKPoint(0.0f, -1.0f), new SKPoint(0.0f, 1.0f)],
        [new SKPoint(-0.6f, -0.35f), new SKPoint(0.0f, -1.0f), new SKPoint(0.6f, -0.35f)]
    ]);

    public static Rune Perth { get; } = new([
        [new SKPoint(-0.5f, -1.0f), new SKPoint(-0.5f, 1.0f)],
        [new SKPoint(-0.5f, -1.0f), new SKPoint(0.15f, -0.45f), new SKPoint(0.6f, -0.9f)],
        [new SKPoint(-0.5f, 1.0f), new SKPoint(0.15f, 0.45f), new SKPoint(0.6f, 0.9f)]
    ]);

    public static Rune Sowilo { get; } = new([
        [new SKPoint(0.4f, -1.0f), new SKPoint(-0.5f, -0.25f), new SKPoint(0.5f, 0.25f), new SKPoint(-0.4f, 1.0f)]
    ]);

    public static Rune Ehwaz { get; } = new([
        [new SKPoint(-0.6f, 1.0f), new SKPoint(-0.6f, -1.0f), new SKPoint(0.0f, -0.35f), new SKPoint(0.6f, -1.0f), new SKPoint(0.6f, 1.0f)]
    ]);

    public static Rune Laguz { get; } = new([
        [new SKPoint(-0.3f, -1.0f), new SKPoint(-0.3f, 1.0f)],
        [new SKPoint(-0.3f, -1.0f), new SKPoint(0.5f, -0.4f)]
    ]);

    public static Rune Dagaz { get; } = new([
        [new SKPoint(-0.8f, -1.0f), new SKPoint(0.8f, 1.0f), new SKPoint(0.8f, -1.0f), new SKPoint(-0.8f, 1.0f), new SKPoint(-0.8f, -1.0f)]
    ]);

    public static Rune Othala { get; } = new([
        [new SKPoint(0.6f, 1.0f), new SKPoint(-0.55f, -0.3f), new SKPoint(0.0f, -1.0f), new SKPoint(0.55f, -0.3f), new SKPoint(-0.6f, 1.0f)]
    ]);

    public static Rune Separator { get; } = new([
        [new SKPoint(0.0f, -0.55f), new SKPoint(0.0f, -0.35f)],
        [new SKPoint(0.0f, 0.35f), new SKPoint(0.0f, 0.55f)]
    ]);
}
