namespace Gallean.Engine;

internal static class StackStepCalculator
{
    public static WorldStackDirection GetDirection(WorldPosition from, WorldPosition to)
    {
        int dx = Math.Sign(to.X - from.X);
        int dy = Math.Sign(to.Y - from.Y);

        return (dx, dy) switch
        {
            (0, -1) => WorldStackDirection.NorthEast,
            (1, -1) => WorldStackDirection.East,
            (1, 0) => WorldStackDirection.SouthEast,
            (1, 1) => WorldStackDirection.South,
            (0, 1) => WorldStackDirection.SouthWest,
            (-1, 1) => WorldStackDirection.West,
            (-1, 0) => WorldStackDirection.NorthWest,
            (-1, -1) => WorldStackDirection.North,
            _ => WorldStackDirection.South
        };
    }

    public static WorldPosition MakeStep(WorldPosition from, WorldStackDirection direction)
    {
        (int dx, int dy) = direction switch
        {
            WorldStackDirection.NorthEast => (0, -1),
            WorldStackDirection.East => (1, -1),
            WorldStackDirection.SouthEast => (1, 0),
            WorldStackDirection.South => (1, 1),
            WorldStackDirection.SouthWest => (0, 1),
            WorldStackDirection.West => (-1, 1),
            WorldStackDirection.NorthWest => (-1, 0),
            WorldStackDirection.North => (-1, -1)
        };

        return new WorldPosition(from.X + dx, from.Y + dy);
    }
}
