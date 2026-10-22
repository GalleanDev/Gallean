using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal static class SkiaHelpers
{
    public static SKPaint MakeImagePaint()
    {
        return new SKPaint { IsAntialias = false, BlendMode = SKBlendMode.SrcOver };
    }

    public static SKPaint MakeFillPaint(Color color)
    {
        return new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = false, Color = color.ToSKColor() };
    }

    public static SKPaint MakeAntialiasFillPaint(Color color)
    {
        return new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true, Color = color.ToSKColor() };
    }

    public static SKPaint MakeStrokePaint(Color color, double width)
    {
        return new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = false, Color = color.ToSKColor(), StrokeWidth = (float)width };
    }

    public static SKPaint MakeAntialiasStrokePaint(Color color, double width)
    {
        return new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = true, Color = color.ToSKColor(), StrokeWidth = (float)width };
    }
}
