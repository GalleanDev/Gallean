using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record ScenarioConfiguration(
    ImmutableArray<ScenarioPlayerConfiguration> HumanPlayers,
    WorldDifficulty Difficulty,
    ScenarioLeader? Leader)
{
    public bool Equals(ScenarioConfiguration? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return HumanPlayers.SequenceEqual(other.HumanPlayers) &&
               Difficulty == other.Difficulty &&
               Equals(Leader, other.Leader);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(HumanPlayers);
        hash.Add(Difficulty);
        hash.Add(Leader);

        return hash.ToHashCode();
    }
}
