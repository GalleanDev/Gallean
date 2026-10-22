using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class SpriteRenderOperation(Rect destination, Rect source, Sprite sprite) : ICustomDrawOperation
{
    public Rect Bounds => destination;

    public bool HitTest(Point point)
    {
        return false;
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return ReferenceEquals(this, other);
    }

    public void Dispose()
    {
    }

    public void Render(ImmediateDrawingContext context)
    {
        using ISkiaSharpApiLease lease = context.GetSkiaApiLease();

        SKRect destinationRect = destination.ToSKRect();
        SKRect sourceRect = source.ToSKRect();

        using SKPaint? paint = CreateOpacityPaint(lease.CurrentOpacity);

        lease.SkCanvas.DrawImage(sprite.Image, sourceRect, destinationRect, RenderSamplingOptions.Linear, paint);
    }

    private static SKPaint? CreateOpacityPaint(double opacity)
    {
        if (opacity < 1)
        {
            byte alpha = (byte)Math.Clamp(Math.Round(opacity * byte.MaxValue), byte.MinValue, byte.MaxValue);

            return new SKPaint { Color = new SKColor(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha) };
        }
        else
        {
            return null;
        }
    }
}
