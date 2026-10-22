using Avalonia;
using static Gallean.Client.BattleRenderState;

namespace Gallean.Client;

internal static class BattleHitTester
{
    public static BattleGridTile? HitTest(IReadOnlyList<HitTarget> targets, Point logical)
    {
        HitTarget? best = null;
        double bestArea = 0;

        foreach (HitTarget target in targets)
        {
            if (target.Bounds.Contains(logical))
            {
                double area = target.Bounds.Width * target.Bounds.Height;

                if (best is null || area <= bestArea)
                {
                    best = target;
                    bestArea = area;
                }
            }
        }

        return best?.Tile;
    }
}
