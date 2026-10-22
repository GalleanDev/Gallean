using Avalonia;

namespace Gallean.Client;

internal static class WorldHitTester
{
    public static IReadOnlyList<WorldPosition> HitTest(IReadOnlyList<WorldRenderContent.HitTarget> targets, Point world)
    {
        List<WorldPosition> positions = [];

        for (int index = targets.Count - 1; index >= 0; index--)
        {
            WorldRenderContent.HitTarget target = targets[index];

            if (target.Bounds.Contains(world))
            {
                positions.Add(target.Position);
            }
        }

        return positions;
    }
}
