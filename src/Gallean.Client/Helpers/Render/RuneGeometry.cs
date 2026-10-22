using SkiaSharp;

namespace Gallean.Client;

internal static class RuneGeometry
{
    private const double RuneWidthRatio = 0.62;
    private const double RuneAdvance = 1.0;
    private const double SeparatorAdvance = 0.5;

    public static SKPath Build(
        IReadOnlyList<IReadOnlyList<Rune>> inscription,
        double innerRadius,
        double outerRadius,
        double strokeWidth)
    {
        SKPathBuilder builder = new();
        builder.FillType = SKPathFillType.Winding;

        double bandCenter = (innerRadius + outerRadius) / 2.0;
        double halfHeight = (outerRadius - innerRadius) / 2.0;
        double halfWidth = halfHeight * RuneWidthRatio;

        double advance = 0;

        foreach (IReadOnlyList<Rune> word in inscription)
        {
            advance += word.Count * RuneAdvance + SeparatorAdvance;
        }

        double degreesPerAdvance = 360.0 / advance;
        double angle = -90.0;

        using SKPaint strokePaint = new();
        strokePaint.Style = SKPaintStyle.Stroke;
        strokePaint.StrokeWidth = (float)strokeWidth;
        strokePaint.StrokeCap = SKStrokeCap.Square;
        strokePaint.StrokeJoin = SKStrokeJoin.Miter;

        using SKPath separatorOutline = BuildOutline(Rune.Separator, halfWidth, halfHeight, strokePaint);

        foreach (IReadOnlyList<Rune> word in inscription)
        {
            foreach (Rune rune in word)
            {
                angle -= RuneAdvance / 2.0 * degreesPerAdvance;

                using SKPath runeOutline = BuildOutline(rune, halfWidth, halfHeight, strokePaint);
                SKMatrix runePlacement = GetPlacement(angle, bandCenter);
                builder.AddPath(runeOutline, in runePlacement, SKPathAddMode.Append);

                angle -= RuneAdvance / 2.0 * degreesPerAdvance;
            }

            angle -= SeparatorAdvance / 2.0 * degreesPerAdvance;

            SKMatrix separatorPlacement = GetPlacement(angle, bandCenter);
            builder.AddPath(separatorOutline, in separatorPlacement, SKPathAddMode.Append);

            angle -= SeparatorAdvance / 2.0 * degreesPerAdvance;
        }

        using SKPath runes = builder.Detach();
        using SKPath band = BuildBand(innerRadius, outerRadius);

        return runes.Op(band, SKPathOp.Intersect);
    }

    private static SKPath BuildOutline(Rune rune, double halfWidth, double halfHeight, SKPaint strokePaint)
    {
        SKPathBuilder builder = new();

        foreach (IReadOnlyList<SKPoint> stroke in rune.Strokes)
        {
            builder.MoveTo((float)(stroke[0].X * halfWidth), (float)(stroke[0].Y * halfHeight));

            for (int index = 1; index < stroke.Count; index++)
            {
                builder.LineTo((float)(stroke[index].X * halfWidth), (float)(stroke[index].Y * halfHeight));
            }

            if (stroke[0] == stroke[^1])
            {
                builder.Close();
            }
        }

        using SKPath centerline = builder.Detach();

        return strokePaint.GetFillPath(centerline);
    }

    private static SKPath BuildBand(double innerRadius, double outerRadius)
    {
        SKPathBuilder builder = new();
        builder.FillType = SKPathFillType.EvenOdd;

        builder.AddCircle(0, 0, (float)outerRadius);
        builder.AddCircle(0, 0, (float)innerRadius);

        return builder.Detach();
    }

    private static SKMatrix GetPlacement(double angleDegrees, double bandCenter)
    {
        double angle = angleDegrees * Math.PI / 180.0;
        float cos = (float)Math.Cos(angle);
        float sin = (float)Math.Sin(angle);

        return new SKMatrix(sin, cos, (float)(bandCenter * cos), -cos, sin, (float)(bandCenter * sin), 0, 0, 1);
    }
}
