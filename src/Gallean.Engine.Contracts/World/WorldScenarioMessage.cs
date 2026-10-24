using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record WorldScenarioMessage(
    int TurnNumber,
    ScenarioEventId EventId,
    ScenarioTextId TextId,
    ScenarioEventPortraitId? PortraitId,
    string? PortraitName,
    bool IsPortraitLeft,
    WorldPlayerId TriggererId,
    ImmutableArray<WorldPlayerId> Audience)
{
    public bool Equals(WorldScenarioMessage? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return TurnNumber == other.TurnNumber &&
               EventId == other.EventId &&
               TextId == other.TextId &&
               PortraitId == other.PortraitId &&
               PortraitName == other.PortraitName &&
               IsPortraitLeft == other.IsPortraitLeft &&
               TriggererId == other.TriggererId &&
               Audience.SequenceEqual(other.Audience);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(TurnNumber);
        hash.Add(EventId);
        hash.Add(TextId);
        hash.Add(PortraitId);
        hash.Add(PortraitName);
        hash.Add(IsPortraitLeft);
        hash.Add(TriggererId);
        hash.AddSequence(Audience);

        return hash.ToHashCode();
    }
}
