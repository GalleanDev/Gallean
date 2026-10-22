using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record WorldMap(int Width, int Height, ImmutableArray<WorldMapCell> Cells)
{
    public bool Equals(WorldMap? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Width == other.Width &&
               Height == other.Height &&
               Cells.SequenceEqual(other.Cells);
    }

    public WorldMapCell GetCellAt(WorldPosition position)
    {
        if (!IsValidPosition(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Position is out of bounds.");
        }

        return Cells[position.Y * Width + position.X];
    }

    public bool IsValidPosition(WorldPosition position)
    {
        return position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
    }

    public WorldMap UpdateCellAt(WorldPosition position, Func<WorldMapCell, WorldMapCell> updateFunc)
    {
        if (!IsValidPosition(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "Position is out of bounds.");
        }

        int index = position.Y * Width + position.X;

        return this with { Cells = Cells.SetItem(index, updateFunc(Cells[index])) };
    }

    public WorldMap UpdateCells(Action<ImmutableArray<WorldMapCell>.Builder> updateFunc)
    {
        ImmutableArray<WorldMapCell>.Builder builder = Cells.ToBuilder();

        updateFunc(builder);

        return this with { Cells = builder.MoveToImmutable() };
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Width);
        hash.Add(Height);
        hash.AddSequence(Cells);

        return hash.ToHashCode();
    }
}
