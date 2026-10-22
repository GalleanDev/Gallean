using Avalonia;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class MinimapRenderer
{
    private const double CapitalHalfWidth = 8.0;
    private const double CityHalfWidth = 5.5;
    private const double StackRadius = 2.5;
    private const double OutlineWidth = 1.5;
    private const double InsetWidth = 1.0;

    public void Render(
        SKCanvas canvas,
        WorldRenderContent content,
        MinimapState minimap,
        MinimapLayout layout,
        IReadOnlyList<Point> viewportCells)
    {
        SKRect destination = new(
            (float)layout.Box.X,
            (float)layout.Box.Y,
            (float)layout.Box.Right,
            (float)layout.Box.Bottom);

        using SKPaint image = SkiaHelpers.MakeImagePaint();
        canvas.DrawImage(content.MinimapImage, destination, RenderSamplingOptions.Linear, image);

        DrawMarkers(canvas, minimap, layout);
        DrawViewport(canvas, layout, viewportCells);
    }

    private static void DrawMarkers(SKCanvas canvas, MinimapState minimap, MinimapLayout layout)
    {
        using SKPaint fill = SkiaHelpers.MakeAntialiasFillPaint(Theme.Minimap.MarkerOutlineColor);
        using SKPaint outline = SkiaHelpers.MakeAntialiasStrokePaint(Theme.Minimap.MarkerOutlineColor, OutlineWidth);
        using SKPaint inset = SkiaHelpers.MakeAntialiasStrokePaint(Theme.Minimap.CapitalInsetColor, InsetWidth);

        foreach (MinimapState.Marker marker in minimap.Markers)
        {
            Point center = layout.CellToScreen(marker.Position.X, marker.Position.Y);
            fill.Color = marker.Color.ToSKColor();

            switch (marker.Type)
            {
                case MinimapState.MarkerType.Capital:
                    DrawFortification(canvas, center, CapitalHalfWidth, fill, outline, inset);
                    break;

                case MinimapState.MarkerType.City:
                    DrawFortification(canvas, center, CityHalfWidth, fill, outline, null);
                    break;

                case MinimapState.MarkerType.Stack:
                    DrawStack(canvas, center, fill, outline);
                    break;
            }
        }
    }

    private static void DrawFortification(
        SKCanvas canvas,
        Point center,
        double halfWidth,
        SKPaint fill,
        SKPaint outline,
        SKPaint? inset)
    {
        using SKPath diamond = MakeDiamond(center, halfWidth);

        canvas.DrawPath(diamond, outline);
        canvas.DrawPath(diamond, fill);

        if (inset is not null)
        {
            using SKPath inner = MakeDiamond(center, halfWidth - OutlineWidth - InsetWidth / 2.0);
            canvas.DrawPath(inner, inset);
        }
    }

    private static void DrawStack(SKCanvas canvas, Point center, SKPaint fill, SKPaint outline)
    {
        canvas.DrawCircle((float)center.X, (float)center.Y, (float)StackRadius, outline);
        canvas.DrawCircle((float)center.X, (float)center.Y, (float)StackRadius, fill);
    }

    private static SKPath MakeDiamond(Point center, double halfWidth)
    {
        float x = (float)center.X;
        float y = (float)center.Y;
        float width = (float)halfWidth;
        float height = (float)(halfWidth * 0.75);
        SKPathBuilder builder = new();

        builder.MoveTo(x, y - height);
        builder.LineTo(x + width, y);
        builder.LineTo(x, y + height);
        builder.LineTo(x - width, y);
        builder.Close();

        return builder.Detach();
    }

    private static void DrawViewport(SKCanvas canvas, MinimapLayout layout, IReadOnlyList<Point> viewportCells)
    {
        if (viewportCells.Count < 2)
        {
            return;
        }

        using SKPaint paint = SkiaHelpers.MakeStrokePaint(Theme.Minimap.ViewportColor, 1.5);

        for (int index = 0; index < viewportCells.Count; index++)
        {
            Point fromCell = viewportCells[index];
            Point toCell = viewportCells[(index + 1) % viewportCells.Count];
            Point from = layout.CellToScreen(fromCell.X, fromCell.Y);
            Point to = layout.CellToScreen(toCell.X, toCell.Y);

            canvas.DrawLine((float)from.X, (float)from.Y, (float)to.X, (float)to.Y, paint);
        }
    }
}
