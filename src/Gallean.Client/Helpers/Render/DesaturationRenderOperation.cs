using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class DesaturationRenderOperation(Rect bounds) : ICustomDrawOperation
{
    private static readonly float[] Luminance =
    [
        0.3f, 0.59f, 0.11f, 0.0f, 0.0f,
        0.3f, 0.59f, 0.11f, 0.0f, 0.0f,
        0.3f, 0.59f, 0.11f, 0.0f, 0.0f,
        0.0f, 0.0f, 0.0f, 1.0f, 0.0f
    ];

    public Rect Bounds => bounds;

    public bool HitTest(Point point)
    {
        return bounds.Contains(point);
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return other is DesaturationRenderOperation operation && operation.Bounds == bounds;
    }

    public void Dispose()
    {
    }

    public void Render(ImmediateDrawingContext context)
    {
        using ISkiaSharpApiLease lease = context.GetSkiaApiLease();

        byte alpha = (byte)Math.Clamp(Math.Round(lease.CurrentOpacity * byte.MaxValue), byte.MinValue, byte.MaxValue);

        if (alpha != 0)
        {
            using SKPaint layerPaint = new()
            {
                Color = new SKColor(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha)
            };

            using SKColorFilter matrix = SKColorFilter.CreateColorMatrix(Luminance);
            using SKImageFilter desaturation = SKImageFilter.CreateColorFilter(matrix);

            lease.SkCanvas.SaveLayer(new SKCanvasSaveLayerRec { Bounds = bounds.ToSKRect(), Paint = layerPaint, Backdrop = desaturation });
            lease.SkCanvas.Restore();
        }
    }
}
