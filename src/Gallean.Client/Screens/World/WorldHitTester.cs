using Avalonia;

namespace Gallean.Client;

internal static class WorldHitTester
{
    public static WorldPosition? HitTest(IReadOnlyList<WorldRenderContent.HitTarget> targets, Point world)
    {
        for (int index = targets.Count - 1; index >= 0; index--)
        {
            WorldRenderContent.HitTarget target = targets[index];
            int column = (int)Math.Floor(world.X - target.Bounds.X);
            int row = (int)Math.Floor(world.Y - target.Bounds.Y);

            if (column >= 0 &&
                row >= 0 &&
                column < target.Mask.Width &&
                row < target.Mask.Height &&
                target.Mask.Get(column, row))
            {
                return target.Position;
            }
        }

        return null;
    }
}
