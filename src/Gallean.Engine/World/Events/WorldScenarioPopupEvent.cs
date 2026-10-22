using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldScenarioPopupEvent(
    ScenarioEventId EventId,
    ScenarioTextId TextId,
    ScenarioEventPortraitId? PortraitId,
    string? PortraitName,
    bool IsPortraitLeft,
    WorldPlayerId TriggererId,
    WorldPosition? TriggererPosition,
    ImmutableArray<WorldPlayerId> Audience) : WorldEvent
{
    public bool Equals(WorldScenarioPopupEvent? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return EventId == other.EventId &&
               TextId == other.TextId &&
               PortraitId == other.PortraitId &&
               PortraitName == other.PortraitName &&
               IsPortraitLeft == other.IsPortraitLeft &&
               TriggererId == other.TriggererId &&
               TriggererPosition == other.TriggererPosition &&
               Audience.SequenceEqual(other.Audience);
    }

    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(EventId);
        hash.Add(TextId);
        hash.Add(PortraitId);
        hash.Add(PortraitName);
        hash.Add(IsPortraitLeft);
        hash.Add(TriggererId);
        hash.Add(TriggererPosition);
        hash.AddSequence(Audience);

        return hash.ToHashCode();
    }
}
