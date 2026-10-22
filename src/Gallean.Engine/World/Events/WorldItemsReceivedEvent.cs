using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldItemsReceivedEvent(
    WorldPlayerId PlayerId,
    WorldUnitGroup Target,
    ImmutableArray<WorldItem> Items) : WorldEvent
{
    public bool Equals(WorldItemsReceivedEvent? other)
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
               Target == other.Target &&
               Items.SequenceEqual(other.Items);
    }

    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(PlayerId);
        hash.Add(Target);
        hash.AddSequence(Items);

        return hash.ToHashCode();
    }
}
