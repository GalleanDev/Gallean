using SkiaSharp;
using static Gallean.Client.Rune;

namespace Gallean.Client;

internal sealed class WorldCursorGeometry
{
    private readonly record struct Dimensions(
        double OuterRingOuterRadius,
        double OuterRingInnerRadius,
        double InnerRingOuterRadius,
        double InnerRingInnerRadius,
        double LongSpikeRadius,
        double ShortSpikeRadius,
        double SpikeBaseRadius,
        double SpikeBaseHalfAngle,
        double RuneStrokeWidth);

    private const double SpikeHalfAngle = 2.2;
    private const int SpikeCount = 8;

    private const double RuneBandOuterRadius = 0.53;
    private const double RuneBandInnerRadius = 0.39;

    private static readonly IReadOnlyList<IReadOnlyList<Rune>> Inscription =
    [
        [Gebo, Othala],
        [Wunjo, Hagalaz, Ehwaz, Raido, Ehwaz],
        [Fehu, Ansuz, Tiwaz, Ehwaz],
        [Laguz, Ehwaz, Ansuz, Dagaz, Sowilo]
    ];

    private WorldCursorGeometry(double rotationDegreesPerSecond, double haloWidth, Dimensions dimensions)
    {
        RotationDegreesPerSecond = rotationDegreesPerSecond;
        HaloWidth = haloWidth;
        GradientInnerRadius = dimensions.InnerRingInnerRadius;
        GradientOuterRadius = dimensions.LongSpikeRadius;
        Frame = BuildFrame(dimensions);
        RuneBand = RuneGeometry.Build(Inscription, RuneBandInnerRadius, RuneBandOuterRadius, dimensions.RuneStrokeWidth);
    }

    public static WorldCursorGeometry Highlight { get; } = new(30.0, 0.09, new Dimensions(
        OuterRingOuterRadius: 0.62,
        OuterRingInnerRadius: 0.57,
        InnerRingOuterRadius: 0.36,
        InnerRingInnerRadius: 0.33,
        LongSpikeRadius: 1.0,
        ShortSpikeRadius: 0.84,
        SpikeBaseRadius: 0.73,
        SpikeBaseHalfAngle: 10.0,
        RuneStrokeWidth: 0.028));

    public double RotationDegreesPerSecond { get; }
    public double HaloWidth { get; }
    public double GradientInnerRadius { get; }
    public double GradientOuterRadius { get; }
    public SKPath Frame { get; }
    public SKPath RuneBand { get; }

    private static SKPath BuildFrame(Dimensions dimensions)
    {
        using SKPath outerRing = BuildRing(dimensions.OuterRingOuterRadius, dimensions.OuterRingInnerRadius);
        using SKPath innerRing = BuildRing(dimensions.InnerRingOuterRadius, dimensions.InnerRingInnerRadius);
        using SKPath spikes = BuildSpikes(dimensions);
        using SKPath spiked = outerRing.Op(spikes, SKPathOp.Union);

        return spiked.Op(innerRing, SKPathOp.Union);
    }

    private static SKPath BuildRing(double outerRadius, double innerRadius)
    {
        SKPathBuilder builder = new();
        builder.FillType = SKPathFillType.EvenOdd;

        builder.AddCircle(0, 0, (float)outerRadius);
        builder.AddCircle(0, 0, (float)innerRadius);

        return builder.Detach();
    }

    private static SKPath BuildSpikes(Dimensions dimensions)
    {
        SKPathBuilder builder = new();
        builder.FillType = SKPathFillType.Winding;

        for (int spike = 0; spike < SpikeCount; spike++)
        {
            double center = spike * 360.0 / SpikeCount;

            AppendPoint(builder, center - dimensions.SpikeBaseHalfAngle, dimensions.OuterRingInnerRadius, move: true);
            AppendPoint(builder, center - SpikeHalfAngle, dimensions.SpikeBaseRadius, move: false);
            AppendPoint(builder, center, GetSpikeRadius(dimensions, spike), move: false);
            AppendPoint(builder, center + SpikeHalfAngle, dimensions.SpikeBaseRadius, move: false);
            AppendPoint(builder, center + dimensions.SpikeBaseHalfAngle, dimensions.OuterRingInnerRadius, move: false);

            builder.Close();
        }

        return builder.Detach();
    }

    private static void AppendPoint(SKPathBuilder builder, double angleDegrees, double radius, bool move)
    {
        double angle = angleDegrees * Math.PI / 180.0;
        float x = (float)(radius * Math.Cos(angle));
        float y = (float)(radius * Math.Sin(angle));

        if (move)
        {
            builder.MoveTo(x, y);
        }
        else
        {
            builder.LineTo(x, y);
        }
    }

    private static double GetSpikeRadius(Dimensions dimensions, int spike)
    {
        if (spike % 2 == 0)
        {
            return dimensions.LongSpikeRadius;
        }
        else
        {
            return dimensions.ShortSpikeRadius;
        }
    }
}
