using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class BattleRenderOperation(
    Rect bounds,
    Matrix transform,
    BattleRenderState state,
    BattleRenderContent content,
    BattleAnimationState? animation,
    BattleWorldTransitionState? transition,
    IAssetProvider assets,
    BattleRenderer renderer,
    TimeSpan loopTime,
    TimeSpan sequenceTime,
    TimeSpan transitionTime,
    float renderResolution,
    bool lightOverlay,
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

        renderer.Render(canvas, content, state, animation, transition, assets, loopTime, sequenceTime, transitionTime, renderResolution, lightOverlay, hitboxOverlay);

        canvas.Restore();

        if (measure)
        {
            diagnostics.AddDraw(Stopwatch.GetElapsedTime(renderStartTimestamp));
        }
    }
}
