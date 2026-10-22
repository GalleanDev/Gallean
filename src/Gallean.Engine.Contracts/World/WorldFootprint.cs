namespace Gallean.Engine.Contracts;

public readonly record struct WorldFootprint(WorldPosition TopLeft, int Width, int Height)
{
    public bool Contains(WorldPosition position)
    {
        return position.X >= TopLeft.X && position.X < TopLeft.X + Width &&
               position.Y >= TopLeft.Y && position.Y < TopLeft.Y + Height;
    }

    public IEnumerable<WorldPosition> EnumerateTiles()
    {
        for (int y = TopLeft.Y; y < TopLeft.Y + Height; y++)
        {
            for (int x = TopLeft.X; x < TopLeft.X + Width; x++)
            {
                yield return new WorldPosition(x, y);
            }
        }
    }
}
