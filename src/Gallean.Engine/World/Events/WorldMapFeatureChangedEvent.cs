using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldMapFeatureChangedEvent(
    ImmutableArray<WorldPosition> Positions,
    WorldMapFeature Feature) : WorldEvent
{
    public bool Equals(WorldMapFeatureChangedEvent? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Positions.SequenceEqual(other.Positions) &&
               Feature == other.Feature;
    }

    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(Positions);
        hash.Add(Feature);

        return hash.ToHashCode();
    }
}
