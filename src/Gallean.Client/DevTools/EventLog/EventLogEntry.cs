using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Client;

internal sealed record EventLogEntry(
    EventLogEntryType Type,
    ImmutableArray<EventLogSpan> Spans,
    ScenarioEventId? ScenarioEventId)
{
    public bool Equals(EventLogEntry? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Type == other.Type &&
               Spans.SequenceEqual(other.Spans) &&
               ScenarioEventId == other.ScenarioEventId;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Type);
        hash.AddSequence(Spans);
        hash.Add(ScenarioEventId);

        return hash.ToHashCode();
    }
}
