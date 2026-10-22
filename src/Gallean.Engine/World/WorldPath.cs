using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldPath
{
    private readonly Lazy<ImmutableArray<WorldPosition>> positions;

    public WorldPath(ImmutableArray<WorldPathSegment> segments)
    {
        Segments = segments;
        Cost = segments.Sum(segment => segment.Cost);

        positions = new Lazy<ImmutableArray<WorldPosition>>(GetPositions);
    }

    public ImmutableArray<WorldPathSegment> Segments { get; }

    public ImmutableArray<WorldPosition> Positions => positions.Value;

    public int Cost { get; }

    public WorldPath Truncate(int segmentCount)
    {
        return new WorldPath(Segments[..segmentCount]);
    }

    public WorldPath Skip(int segmentCount)
    {
        return new WorldPath(Segments[segmentCount..]);
    }

    public bool Equals(WorldPath? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Segments.SequenceEqual(other.Segments);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(Segments);

        return hash.ToHashCode();
    }

    private ImmutableArray<WorldPosition> GetPositions()
    {
        ImmutableArray<WorldPosition>.Builder result = ImmutableArray.CreateBuilder<WorldPosition>(Segments.Length);

        foreach (WorldPathSegment segment in Segments)
        {
            result.Add(segment.End);
        }

        return result.ToImmutable();
    }
}
