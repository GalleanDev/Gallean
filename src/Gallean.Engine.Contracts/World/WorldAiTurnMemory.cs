using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

[method: JsonConstructor]
public sealed record WorldAiTurnMemory(
    int TurnNumber,
    WorldPlayerId PlayerId,
    int ActionsChosen,
    int UnitsPurchased,
    int BuildingsConstructed,
    ImmutableSortedSet<WorldStackId> ExhaustedStacks)
{
    public WorldAiTurnMemory(int turnNumber, WorldPlayerId playerId)
        : this(turnNumber, playerId, 0, 0, 0, [])
    {
    }

    public bool Equals(WorldAiTurnMemory? other)
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
               PlayerId == other.PlayerId &&
               ActionsChosen == other.ActionsChosen &&
               UnitsPurchased == other.UnitsPurchased &&
               BuildingsConstructed == other.BuildingsConstructed &&
               ExhaustedStacks.SequenceEqual(other.ExhaustedStacks);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(TurnNumber);
        hash.Add(PlayerId);
        hash.Add(ActionsChosen);
        hash.Add(UnitsPurchased);
        hash.Add(BuildingsConstructed);
        hash.AddSequence(ExhaustedStacks);

        return hash.ToHashCode();
    }
}
