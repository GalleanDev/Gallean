using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class CapitalBackdropRenderOperation(
    Rect bounds,
    Rect viewport,
    Sprite sprite,
    double blurStrength,
    double edgeFadeWidth) : ICustomDrawOperation
{
    public Rect Bounds => bounds;

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

        SKCanvas canvas = lease.SkCanvas;

        SKMatrix transform = SKMatrix.CreateScaleTranslation(
            (float)(viewport.Width / sprite.Image.Width),
            (float)(viewport.Height / sprite.Image.Height),
            (float)viewport.X,
            (float)viewport.Y);

        byte alpha = (byte)Math.Clamp(Math.Round(lease.CurrentOpacity * byte.MaxValue), byte.MinValue, byte.MaxValue);

        using SKPaint layerPaint = new()
        {
            Color = new SKColor(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha)
        };

        using SKShader shader = sprite.Image.ToShader(SKShaderTileMode.Mirror, SKShaderTileMode.Mirror, RenderSamplingOptions.Linear, transform);
        using SKImageFilter blur = SKImageFilter.CreateBlur((float)blurStrength, (float)blurStrength, SKShaderTileMode.Clamp);
        using SKPaint paint = new()
        {
            Shader = shader,
            ImageFilter = blur
        };

        using SKShader edgeFadeShader = CreateEdgeFadeShader();
        using SKPaint edgeFadePaint = new()
        {
            Shader = edgeFadeShader,
            BlendMode = SKBlendMode.DstOut
        };

        canvas.SaveLayer(bounds.ToSKRect(), layerPaint);
        canvas.DrawRect(bounds.ToSKRect(), paint);
        canvas.DrawRect(viewport.ToSKRect(), edgeFadePaint);
        canvas.Restore();
    }

    private SKShader CreateEdgeFadeShader()
    {
        SKColor opaque = Theme.CapitalScene.EdgeMaskColor.ToSKColor();
        SKColor transparent = Theme.CapitalScene.EdgeMaskFadeColor.ToSKColor();
        SKColor[] colors = [transparent, opaque, opaque, transparent];
        float[] positions = [0.0f, (float)edgeFadeWidth, (float)(1 - edgeFadeWidth), 1.0f];
        SKPoint topLeft = viewport.TopLeft.ToSKPoint();

        using SKShader horizontal = SKShader.CreateLinearGradient(topLeft, viewport.TopRight.ToSKPoint(), colors, positions, SKShaderTileMode.Clamp);
        using SKShader vertical = SKShader.CreateLinearGradient(topLeft, viewport.BottomLeft.ToSKPoint(), colors, positions, SKShaderTileMode.Clamp);

        return SKShader.CreateCompose(horizontal, vertical, SKBlendMode.DstIn);
    }
}
