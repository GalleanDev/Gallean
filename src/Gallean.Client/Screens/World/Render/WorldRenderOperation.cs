using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class WorldRenderOperation(
    Rect bounds,
    Matrix transform,
    WorldRenderState state,
    WorldRenderContent content,
    WorldRenderer renderer,
    IAssetProvider assets,
    TimeSpan loopTime,
    TimeSpan animationTime,
    float renderResolution,
    bool hitboxOverlay,
    DiagnosticsRecorder diagnostics) : ICustomDrawOperation
{
    private readonly SKMatrix skiaTransform = transform.ToSKMatrix();

    public Rect Bounds => bounds;

    public bool HitTest(Point point)
    {
        return bounds.Contains(point);
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

        canvas.Save();
        canvas.Concat(in skiaTransform);

        bool measure = diagnostics.IsEnabled;
        long renderStartTimestamp = measure ? Stopwatch.GetTimestamp() : 0;

        renderer.Render(canvas, content, state, assets, loopTime, animationTime, renderResolution, hitboxOverlay);

        canvas.Restore();

        if (measure)
        {
            diagnostics.AddDraw(Stopwatch.GetElapsedTime(renderStartTimestamp));
        }
    }
}
