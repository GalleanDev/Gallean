using SkiaSharp;
using static Gallean.Client.Rune;

namespace Gallean.Client;

internal static class WorldObjectiveMarkerGeometry
{
    public const double FootprintRadius = 0.92;

    private const double RingOuterRadius = 1.0;
    private const double RingInnerRadius = 0.959;

    private const double BandOuterRadius = 0.918;
    private const double BandInnerRadius = 0.837;

    private const double RuneStrokeWidth = 0.015;

    private static readonly IReadOnlyList<IReadOnlyList<Rune>> Inscription =
    [
        [Sowilo, Ehwaz, Ehwaz, Kaunan],
        [Ansuz, Naudiz, Dagaz],
        [Hagalaz, Othala, Laguz, Dagaz],
        [Thurisaz, Isaz, Sowilo],
        [Perth, Laguz, Ansuz, Kaunan, Ehwaz],
        [Fehu, Othala, Raido],
        [Thurisaz, Ehwaz],
        [Gebo, Othala, Dagaz, Sowilo],
        [Wunjo, Ansuz, Tiwaz, Kaunan, Hagalaz],
        [Othala, Uruz, Ehwaz, Raido],
        [Thurisaz, Ehwaz],
        [Sowilo, Ansuz, Kaunan, Raido, Ehwaz, Dagaz],
        [Laguz, Ansuz, Naudiz, Dagaz, Sowilo],
        [Othala, Fehu],
        [Naudiz, Ehwaz, Uruz, Ehwaz, Naudiz, Dagaz, Ansuz, Ansuz, Raido]
    ];

    public static SKPath Ring { get; } = BuildRing(RingOuterRadius, RingInnerRadius);
    public static SKPath Band { get; } = BuildRing(BandOuterRadius, BandInnerRadius);
    public static SKPath RuneBand { get; } = RuneGeometry.Build(Inscription, BandInnerRadius, BandOuterRadius, RuneStrokeWidth);

    private static SKPath BuildRing(double outerRadius, double innerRadius)
    {
        SKPathBuilder builder = new();
        builder.FillType = SKPathFillType.EvenOdd;

        builder.AddCircle(0, 0, (float)outerRadius);
        builder.AddCircle(0, 0, (float)innerRadius);

        return builder.Detach();
    }
}
