using SkiaSharp;
using static Gallean.Client.BattleRenderState;

namespace Gallean.Client;

internal sealed partial class BattleRenderer
{
    private static void DrawHitboxOverlay(SKCanvas canvas, BattleRenderState state)
    {
        using SKPaint fillPaint = SkiaHelpers.MakeFillPaint(Theme.DevTools.HitboxFillColor);
        using SKPaint strokePaint = SkiaHelpers.MakeStrokePaint(Theme.DevTools.HitboxStrokeColor, 1);

        foreach (HitTarget target in state.HitTargets)
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
