using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal static class OrnamentShape
{
    private const int TaperSteps = 16;
    private const double TaperExponent = 0.55;
    private const int SwashSteps = 32;
    private const double SwashTipWidth = 0.07;
    private const int WaveStepsPerPeriod = 48;

    public static Geometry Diamond()
    {
        StreamGeometry geometry = new();

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new Point(0, -1));
            context.LineTo(new Point(1, 0));
            context.LineTo(new Point(0, 1));
            context.LineTo(new Point(-1, 0));
            context.EndFigure(true);
        }

        return geometry;
    }

    public static Geometry Spike(double length, double width)
    {
        StreamGeometry geometry = new();

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new Point(0, HalfWidth(width, 0)));

            for (int step = 1; step <= TaperSteps; step++)
            {
                double progress = (double)step / TaperSteps;
                context.LineTo(new Point(length * progress, HalfWidth(width, progress)));
            }

            for (int step = TaperSteps - 1; step >= 0; step--)
            {
                double progress = (double)step / TaperSteps;
                context.LineTo(new Point(length * progress, -HalfWidth(width, progress)));
            }

            context.EndFigure(true);
        }

        return geometry;
    }

    public static Geometry Leaf(double length, double width)
    {
        StreamGeometry geometry = new();

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new Point(0, 0));
            context.QuadraticBezierTo(new Point(length / 2.0, width), new Point(length, 0));
            context.QuadraticBezierTo(new Point(length / 2.0, -width), new Point(0, 0));
            context.EndFigure(true);
        }

        return geometry;
    }

    public static Geometry Swash(double length, double height, double width)
    {
        StreamGeometry geometry = new();
        Point[] spine = [new(0.0, 1.0), new(0.3, 0.97), new(0.55, 0.3), new(1.18, 0.04)];

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(SwashEdge(spine, length, height, width, 0, 1), true);

            for (int step = 1; step <= SwashSteps; step++)
            {
                context.LineTo(SwashEdge(spine, length, height, width, (double)step / SwashSteps, 1));
            }

            for (int step = SwashSteps; step >= 0; step--)
            {
                context.LineTo(SwashEdge(spine, length, height, width, (double)step / SwashSteps, -1));
            }

            context.EndFigure(true);
        }

        return geometry;
    }

    public static Geometry Wave(double periods, double period, double height, double crest)
    {
        StreamGeometry geometry = new();
        int steps = (int)Math.Round(periods * WaveStepsPerPeriod);
        double skew = Math.Log(0.5) / Math.Log(crest);

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new Point(0, height), false);

            for (int step = 1; step <= steps; step++)
            {
                double phase = (double)(step % WaveStepsPerPeriod) / WaveStepsPerPeriod;
                double lift = 0.5 - (0.5 * Math.Cos(2 * Math.PI * Math.Pow(phase, skew)));
                context.LineTo(new Point(period * step / WaveStepsPerPeriod, height * (1 - lift)));
            }

            context.EndFigure(false);
        }

        return geometry;
    }

    public static Geometry Arc(Point center, double radius, double startAngle, double sweepAngle)
    {
        StreamGeometry geometry = new();

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(Polar(center, radius, startAngle), false);
            context.ArcTo(Polar(center, radius, startAngle + sweepAngle), new Size(radius, radius), 0, sweepAngle > 180, SweepDirection.Clockwise);
            context.EndFigure(false);
        }

        return geometry;
    }

    public static Point Polar(Point center, double radius, double angle)
    {
        double radians = angle * Math.PI / 180;

        return new Point(center.X + (radius * Math.Sin(radians)), center.Y - (radius * Math.Cos(radians)));
    }

    private static Point SwashEdge(
        Point[] spine,
        double length,
        double height,
        double width,
        double progress,
        double side)
    {
        double rest = 1 - progress;
        double x = (rest * rest * rest * spine[0].X) + (3 * rest * rest * progress * spine[1].X) + (3 * rest * progress * progress * spine[2].X) + (progress * progress * progress * spine[3].X);
        double y = (rest * rest * rest * spine[0].Y) + (3 * rest * rest * progress * spine[1].Y) + (3 * rest * progress * progress * spine[2].Y) + (progress * progress * progress * spine[3].Y);
        double tangentX = ((3 * rest * rest * (spine[1].X - spine[0].X)) + (6 * rest * progress * (spine[2].X - spine[1].X)) + (3 * progress * progress * (spine[3].X - spine[2].X))) * length;
        double tangentY = ((3 * rest * rest * (spine[1].Y - spine[0].Y)) + (6 * rest * progress * (spine[2].Y - spine[1].Y)) + (3 * progress * progress * (spine[3].Y - spine[2].Y))) * height;
        double tangentLength = Math.Sqrt((tangentX * tangentX) + (tangentY * tangentY));
        double swell = Math.Pow(Math.Sin(Math.PI * progress), 0.9) * Math.Pow(rest, 0.25);
        double offset = side * width * (SwashTipWidth + ((1 - SwashTipWidth) * swell)) / 2.0;

        return new Point((x * length) - (tangentY / tangentLength * offset), (y * height) + (tangentX / tangentLength * offset));
    }

    private static double HalfWidth(double width, double progress)
    {
        return width * Math.Pow(1 - progress, TaperExponent) / 2.0;
    }
}
