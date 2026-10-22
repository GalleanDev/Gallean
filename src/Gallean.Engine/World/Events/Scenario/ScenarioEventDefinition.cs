using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record ScenarioEventDefinition(
    ScenarioEventId Id,
    ImmutableArray<WorldRace> AffectedRaces,
    ImmutableArray<WorldRace> VerifiedRaces,
    ImmutableArray<ScenarioEventCondition> Conditions,
    ImmutableArray<ScenarioEventEffect> Effects,
    bool Enabled,
    bool OccurOnce,
    int Chance,
    int Order)
{
    public bool Equals(ScenarioEventDefinition? other)
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
               AffectedRaces.SequenceEqual(other.AffectedRaces) &&
               VerifiedRaces.SequenceEqual(other.VerifiedRaces) &&
               Conditions.SequenceEqual(other.Conditions) &&
               Effects.SequenceEqual(other.Effects) &&
               Enabled == other.Enabled &&
               OccurOnce == other.OccurOnce &&
               Chance == other.Chance &&
               Order == other.Order;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Id);
        hash.AddSequence(AffectedRaces);
        hash.AddSequence(VerifiedRaces);
        hash.AddSequence(Conditions);
        hash.AddSequence(Effects);
        hash.Add(Enabled);
        hash.Add(OccurOnce);
        hash.Add(Chance);
        hash.Add(Order);

        return hash.ToHashCode();
    }
}
