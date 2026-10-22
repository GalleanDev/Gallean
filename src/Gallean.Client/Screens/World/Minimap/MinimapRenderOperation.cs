using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class MinimapRenderOperation(
    Rect bounds,
    WorldRenderContent content,
    MinimapState minimap,
    MinimapLayout layout,
    IReadOnlyList<Point> viewportCells,
    MinimapRenderer renderer) : ICustomDrawOperation
{
    public Rect Bounds => bounds;

    public bool HitTest(Point point)
    {
        double dx = (point.X - bounds.Center.X) / (bounds.Width / 2.0);
        double dy = (point.Y - bounds.Center.Y) / (bounds.Height / 2.0);

        return (dx * dx) + (dy * dy) <= 1;
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
        using SKRoundRect rect = new(bounds.ToSKRect(), (float)(bounds.Width / 2.0), (float)(bounds.Height / 2.0));

        canvas.Save();
        canvas.ClipRoundRect(rect, antialias: true);
        renderer.Render(canvas, content, minimap, layout, viewportCells);
        canvas.Restore();
    }
}
