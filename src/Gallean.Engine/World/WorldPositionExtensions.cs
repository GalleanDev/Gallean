namespace Gallean.Engine;

public static class WorldPositionExtensions
{
    extension(WorldPosition position)
    {
        public IEnumerable<WorldPosition> GetAdjacentPositions()
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx != 0 || dy != 0)
                    {
                        yield return new WorldPosition(position.X + dx, position.Y + dy);
                    }
                }
            }
        }

        public int GetDistance(WorldPosition target)
        {
            return Math.Max(Math.Abs(position.X - target.X), Math.Abs(position.Y - target.Y));
        }

        public bool IsAdjacentTo(WorldPosition target)
        {
            return position.GetDistance(target) == 1;
        }
    }
}
