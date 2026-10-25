using Avalonia;

namespace Gallean.Client;

internal static class WorldHitTester
{
    public static WorldPosition? HitTest(IReadOnlyList<WorldRenderContent.HitTarget> targets, Point world)
    {
        for (int index = targets.Count - 1; index >= 0; index--)
        {
            WorldRenderContent.HitTarget target = targets[index];

            if (target.Bounds.Contains(world))
            {
                return target.Position;
            }
        }

        return null;
    }
}
