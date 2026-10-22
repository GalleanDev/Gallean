using SkiaSharp;

namespace Gallean.Client;

internal sealed class ScaledSurface : IDisposable
{
    private readonly SKSurface surface;
    private readonly SKRectI bounds;
    private readonly float scale;

    private ScaledSurface(SKSurface surface, SKRectI bounds, float scale)
    {
        this.surface = surface;
        this.bounds = bounds;
        this.scale = scale;
    }

    public SKCanvas Canvas => surface.Canvas;

    public void Dispose()
    {
        surface.Dispose();
    }

    public static ScaledSurface Create(SKCanvas target, float scale, SKMatrix deviceTransform)
    {
        SKRectI bounds = target.DeviceClipBounds;
        int width = Math.Max((int)Math.Ceiling(bounds.Width * scale), 1);
        int height = Math.Max((int)Math.Ceiling(bounds.Height * scale), 1);
        SKImageInfo info = new(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
        GRRecordingContext? context = target.Context;

        SKSurface surface = context is not null ? SKSurface.Create(context, true, info) : SKSurface.Create(info);

        SKMatrix deviceToSurface = SKMatrix.Concat(
            SKMatrix.CreateScale(scale, scale),
            SKMatrix.CreateTranslation(-bounds.Left, -bounds.Top));

        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.SetMatrix(SKMatrix.Concat(deviceToSurface, SKMatrix.Concat(deviceTransform, target.TotalMatrix)));

        return new ScaledSurface(surface, bounds, scale);
    }

    public void DrawTo(SKCanvas target)
    {
        using SKImage image = surface.Snapshot();

        SKRect destination = SKRect.Create(bounds.Left, bounds.Top, image.Width / scale, image.Height / scale);

        target.Save();
        target.ResetMatrix();
        target.DrawImage(image, destination, RenderSamplingOptions.Linear);
        target.Restore();
    }

    public SKShader ToShader()
    {
        using SKImage image = surface.Snapshot();

        SKMatrix surfaceToDevice = SKMatrix.CreateScaleTranslation(1.0f / scale, 1.0f / scale, bounds.Left, bounds.Top);

        return image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, RenderSamplingOptions.Linear, surfaceToDevice);
    }
}
