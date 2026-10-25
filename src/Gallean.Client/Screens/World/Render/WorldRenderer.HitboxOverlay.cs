using Avalonia;
using Gallean.Common;
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
                DrawHitMask(canvas, fillPaint, target.Bounds, target.Mask);

                SKRect rect = new(
                    (float)target.Bounds.Left,
                    (float)target.Bounds.Top,
                    (float)target.Bounds.Right,
                    (float)target.Bounds.Bottom);

                canvas.DrawRect(rect, strokePaint);
            }
        }
    }

    private static void DrawHitMask(SKCanvas canvas, SKPaint paint, Rect bounds, ImmutableBitmap mask)
    {
        float left = (float)bounds.Left;
        float top = (float)bounds.Top;

        for (int row = 0; row < mask.Height; row++)
        {
            int runStart = 0;

            for (int column = 0; column <= mask.Width; column++)
            {
                bool isSet = column < mask.Width && mask.Get(column, row);

                if (!isSet)
                {
                    if (column > runStart)
                    {
                        canvas.DrawRect(new SKRect(left + runStart, top + row, left + column, top + row + 1), paint);
                    }

                    runStart = column + 1;
                }
            }
        }
    }
}
