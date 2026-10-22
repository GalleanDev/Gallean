using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record WorldPlayer(
    WorldPlayerId Id,
    ScenarioTextId NameTextId,
    LordDefinitionId LordId,
    WorldRace Race,
    WorldObjectVariant FaceVariant,
    WorldAiAttitudeType AiAttitude,
    bool IsAi,
    WorldResourceSet Resources,
    WorldPlayerBuildings Buildings,
    WorldPlayerSpellbook Spellbook,
    ImmutableBitmap FogOfWar,
    ImmutableSortedSet<WorldPlayerId> SpiedPlayers,
    ImmutableSortedSet<WorldFortificationId> SpiedRuins,
    ImmutableArray<WorldDiplomacyProposal> IncomingDiplomacyProposals,
    WorldPlayerTurnActions TurnActions)
{
    public bool Equals(WorldPlayer? other)
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
               NameTextId == other.NameTextId &&
               LordId == other.LordId &&
               Race == other.Race &&
               FaceVariant == other.FaceVariant &&
               AiAttitude == other.AiAttitude &&
               IsAi == other.IsAi &&
               Resources == other.Resources &&
               Buildings.Equals(other.Buildings) &&
               Spellbook.Equals(other.Spellbook) &&
               FogOfWar.Equals(other.FogOfWar) &&
               SpiedPlayers.SequenceEqual(other.SpiedPlayers) &&
               SpiedRuins.SequenceEqual(other.SpiedRuins) &&
               IncomingDiplomacyProposals.SequenceEqual(other.IncomingDiplomacyProposals) &&
               TurnActions.Equals(other.TurnActions);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Id);
        hash.Add(NameTextId);
        hash.Add(LordId);
        hash.Add(Race);
        hash.Add(FaceVariant);
        hash.Add(AiAttitude);
        hash.Add(IsAi);
        hash.Add(Resources);
        hash.Add(Buildings);
        hash.Add(Spellbook);
        hash.Add(FogOfWar);
        hash.AddSequence(SpiedPlayers);
        hash.AddSequence(SpiedRuins);
        hash.AddSequence(IncomingDiplomacyProposals);
        hash.Add(TurnActions);

        return hash.ToHashCode();
    }
}
