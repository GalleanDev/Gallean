namespace Gallean.Engine;

internal static class ScenarioLocationCalculator
{
    public static bool Contains(ScenarioLocationDefinition location, WorldPosition position)
    {
        WorldPosition center = location.Position;

        return position.GetDistance(center) <= location.Radius;
    }

    public static IEnumerable<WorldPosition> EnumerateTiles(ScenarioLocationDefinition location, WorldMap map)
    {
        return EnumerateTiles(location.Position, location.Radius, map);
    }

    public static IEnumerable<WorldPosition> EnumerateTiles(WorldPosition center, int radius, WorldMap map)
    {
        int minY = Math.Max(0, center.Y - radius);
        int maxY = Math.Min(map.Height - 1, center.Y + radius);
        int minX = Math.Max(0, center.X - radius);
        int maxX = Math.Min(map.Width - 1, center.X + radius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                WorldPosition position = new(x, y);

                if (position.GetDistance(center) <= radius)
                {
                    yield return position;
                }
            }
        }
    }
}
