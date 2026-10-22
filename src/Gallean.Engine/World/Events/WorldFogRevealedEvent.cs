using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldFogRevealedEvent(
    WorldPlayerId PlayerId,
    ImmutableArray<WorldPosition> Positions) : WorldEvent
{
    public bool Equals(WorldFogRevealedEvent? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return PlayerId == other.PlayerId &&
               Positions.SequenceEqual(other.Positions);
    }

    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(PlayerId);
        hash.AddSequence(Positions);

        return hash.ToHashCode();
    }
}
