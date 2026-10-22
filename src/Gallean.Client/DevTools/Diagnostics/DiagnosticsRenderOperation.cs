using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

namespace Gallean.Client;

internal sealed class DiagnosticsRenderOperation(Rect bounds, DiagnosticsPanel panel, DiagnosticsRecorder recorder) : ICustomDrawOperation
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

        panel.Draw(lease.SkCanvas, bounds.ToSKRect(), recorder);
    }
}
