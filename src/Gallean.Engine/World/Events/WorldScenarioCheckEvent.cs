using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldScenarioCheckEvent(
    ScenarioEventTrigger Trigger,
    ImmutableSortedSet<ScenarioEventId> TriggeredEvents) : WorldEvent
{
    public bool Equals(WorldScenarioCheckEvent? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Trigger == other.Trigger &&
               TriggeredEvents.SequenceEqual(other.TriggeredEvents);
    }

    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Trigger);
        hash.AddSequence(TriggeredEvents);

        return hash.ToHashCode();
    }
}
