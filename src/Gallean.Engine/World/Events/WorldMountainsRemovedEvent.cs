using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldMountainsRemovedEvent(
    ScenarioLocationId Location,
    ImmutableArray<WorldMountainId> MountainIds) : WorldEvent
{
    public bool Equals(WorldMountainsRemovedEvent? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Location == other.Location &&
               MountainIds.SequenceEqual(other.MountainIds);
    }

    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Location);
        hash.AddSequence(MountainIds);

        return hash.ToHashCode();
    }
}
