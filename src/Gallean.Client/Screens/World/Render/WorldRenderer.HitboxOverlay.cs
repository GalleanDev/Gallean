using Avalonia;
using SkiaSharp;
using static Gallean.Client.WorldRenderContent;

namespace Gallean.Client;

internal sealed partial class WorldRenderer
{
    private static void DrawHitboxOverlay(SKCanvas canvas, WorldRenderContent content, Rect visible)
    {
        using SKPaint fillPaint = SkiaHelpers.MakeFillPaint(Theme.DevTools.HitboxFillColor);
        using SKPaint strokePaint = SkiaHelpers.MakeStrokePaint(Theme.DevTools.HitboxStrokeColor, 1);

        foreach (HitTarget target in content.HitTargets)
        {
            if (visible.Intersects(target.Bounds))
            {
                SKRect rect = new(
                    (float)target.Bounds.Left,
                    (float)target.Bounds.Top,
                    (float)target.Bounds.Right,
                    (float)target.Bounds.Bottom);

                canvas.DrawRect(rect, fillPaint);
                canvas.DrawRect(rect, strokePaint);
            }
        }
    }
}
