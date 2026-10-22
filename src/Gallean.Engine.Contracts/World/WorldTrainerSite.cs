using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record WorldTrainerSite(
    WorldSiteId Id,
    WorldFootprint Footprint,
    WorldObjectVariant Variant,
    ScenarioTextId TitleTextId,
    ScenarioTextId GreetingTextId,
    ImmutableSortedSet<WorldPlayerId> VisitorIds,
    int AiPriority) : WorldSite(Id, Footprint, Variant, TitleTextId, GreetingTextId, VisitorIds, AiPriority)
{
    public bool Equals(WorldTrainerSite? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id &&
               Footprint == other.Footprint &&
               Variant == other.Variant &&
               TitleTextId == other.TitleTextId &&
               GreetingTextId == other.GreetingTextId &&
               VisitorIds.SequenceEqual(other.VisitorIds) &&
               AiPriority == other.AiPriority;
    }

    public override TResult Accept<TContext, TResult>(IWorldSiteVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Id);
        hash.Add(Footprint);
        hash.Add(Variant);
        hash.Add(TitleTextId);
        hash.Add(GreetingTextId);
        hash.AddSequence(VisitorIds);
        hash.Add(AiPriority);

        return hash.ToHashCode();
    }
}
