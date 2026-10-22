using SkiaSharp;
using static Gallean.Client.Rune;

namespace Gallean.Client;

internal static class BattleMarkerGeometry
{
    public const double RotationDegreesPerSecond = 90.0;

    private const double PlateRadius = 1.0;
    private const double LongSpikeRadius = 1.47;
    private const double ShortSpikeRadius = 1.31;
    private const double SpikeHalfAngle = 10.5;
    private const int SpikeCount = 8;

    private const double RingRadius = 0.915;
    private const double RingGapRadius = 0.878;

    private const double RuneBandOuterRadius = 0.862;
    private const double RuneBandInnerRadius = 0.672;
    private const double RuneStrokeWidth = 0.034;

    private const double CoreRadius = 0.652;
    private const double CoreGapRadius = 0.616;

    private const double StarTipRadius = 0.572;
    private const double StarBaseRadius = 0.302;
    private const double StarHalfAngle = 24.0;
    private const double StarOffsetAngle = 45.0;
    private const int StarPoints = 4;

    private const double CenterRadius = 0.214;
    private const double CenterGapRadius = 0.152;

    private const int ArcSegments = 12;

    private static readonly IReadOnlyList<IReadOnlyList<Rune>> Inscription =
    [
        [Sowilo, Tiwaz, Raido, Isaz, Kaunan, Ehwaz],
        [Tiwaz, Raido, Uruz, Ehwaz],
        [Ansuz, Naudiz, Dagaz],
        [Sowilo, Tiwaz, Ansuz, Naudiz, Dagaz],
        [Fehu, Ansuz, Sowilo, Tiwaz]
    ];

    public static SKPath Plate { get; } = BuildPlate();
    public static SKPath Disc { get; } = BuildDisc();
    public static SKPath Ring { get; } = BuildRing();

    private static SKPath BuildPlate()
    {
        SKPathBuilder builder = new();

        for (int spike = 0; spike < SpikeCount; spike++)
        {
            double center = spike * 360.0 / SpikeCount;
            double next = center + 360.0 / SpikeCount;

            AppendPoint(builder, center - SpikeHalfAngle, PlateRadius, move: spike == 0);
            AppendPoint(builder, center, GetSpikeRadius(spike), move: false);
            AppendPoint(builder, center + SpikeHalfAngle, PlateRadius, move: false);

            AppendArc(builder, center + SpikeHalfAngle, next - SpikeHalfAngle, PlateRadius);
        }

        builder.Close();

        return builder.Detach();
    }

    private static SKPath BuildDisc()
    {
        using SKPath bands = BuildBands();
        using SKPath runes = RuneGeometry.Build(Inscription, RuneBandInnerRadius, RuneBandOuterRadius, RuneStrokeWidth);
        using SKPath banded = Plate.Op(bands, SKPathOp.Difference);

        return banded.Op(runes, SKPathOp.Difference);
    }

    private static SKPath BuildRing()
    {
        SKPathBuilder builder = new();
        builder.AddCircle(0, 0, (float)RingRadius);

        using SKPath core = builder.Detach();

        return Plate.Op(core, SKPathOp.Difference);
    }

    private static SKPath BuildBands()
    {
        SKPathBuilder builder = new();
        builder.FillType = SKPathFillType.EvenOdd;

        builder.AddCircle(0, 0, (float)RingRadius);
        builder.AddCircle(0, 0, (float)RingGapRadius);
        builder.AddCircle(0, 0, (float)CoreRadius);
        builder.AddCircle(0, 0, (float)CoreGapRadius);
        builder.AddCircle(0, 0, (float)StarTipRadius);
        builder.AddCircle(0, 0, (float)StarBaseRadius);
        builder.AddCircle(0, 0, (float)CenterRadius);
        builder.AddCircle(0, 0, (float)CenterGapRadius);

        AppendStarPoints(builder);

        return builder.Detach();
    }

    private static void AppendStarPoints(SKPathBuilder builder)
    {
        for (int point = 0; point < StarPoints; point++)
        {
            double center = StarOffsetAngle + point * 360.0 / StarPoints;

            AppendPoint(builder, center - StarHalfAngle, StarBaseRadius, move: true);
            AppendPoint(builder, center, StarTipRadius, move: false);
            AppendPoint(builder, center + StarHalfAngle, StarBaseRadius, move: false);

            builder.Close();
        }
    }

    private static void AppendArc(SKPathBuilder builder, double fromAngle, double toAngle, double radius)
    {
        for (int step = 0; step <= ArcSegments; step++)
        {
            AppendPoint(builder, fromAngle + (toAngle - fromAngle) * step / ArcSegments, radius, move: false);
        }
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

    private static double GetSpikeRadius(int spike)
    {
        if (spike % 2 == 0)
        {
            return LongSpikeRadius;
        }
        else
        {
            return ShortSpikeRadius;
        }
    }
}
