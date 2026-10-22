using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record WorldState(
    WorldMap Map,
    WorldTurnInfo TurnInfo,
    WorldDifficulty Difficulty,
    ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> Players,
    ImmutableSortedDictionary<WorldRelationId, WorldRelation> Relations,
    ImmutableSortedDictionary<WorldStackId, WorldStack> Stacks,
    ImmutableSortedDictionary<WorldFortificationId, WorldFortification> Fortifications,
    ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode> ResourceNodes,
    ImmutableSortedDictionary<WorldRodId, WorldRod> Rods,
    ImmutableSortedDictionary<WorldBagId, WorldBag> Bags,
    ImmutableSortedDictionary<WorldTombId, WorldTomb> Tombs,
    ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark> Landmarks,
    ImmutableSortedDictionary<WorldMountainId, WorldMountain> Mountains,
    ImmutableSortedDictionary<WorldSiteId, WorldSite> Sites,
    ImmutableSortedDictionary<WorldSubraceId, WorldSubrace> Subraces,
    WorldScenarioState ScenarioState,
    ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory> AiMemories)
{
    public bool Equals(WorldState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Map == other.Map &&
               TurnInfo == other.TurnInfo &&
               Difficulty == other.Difficulty &&
               Players.SequenceEqual(other.Players) &&
               Relations.SequenceEqual(other.Relations) &&
               Stacks.SequenceEqual(other.Stacks) &&
               Fortifications.SequenceEqual(other.Fortifications) &&
               ResourceNodes.SequenceEqual(other.ResourceNodes) &&
               Rods.SequenceEqual(other.Rods) &&
               Bags.SequenceEqual(other.Bags) &&
               Tombs.SequenceEqual(other.Tombs) &&
               Landmarks.SequenceEqual(other.Landmarks) &&
               Mountains.SequenceEqual(other.Mountains) &&
               Sites.SequenceEqual(other.Sites) &&
               Subraces.SequenceEqual(other.Subraces) &&
               ScenarioState.Equals(other.ScenarioState) &&
               AiMemories.SequenceEqual(other.AiMemories);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Map);
        hash.Add(TurnInfo);
        hash.Add(Difficulty);
        hash.AddSequence(Players);
        hash.AddSequence(Relations);
        hash.AddSequence(Stacks);
        hash.AddSequence(Fortifications);
        hash.AddSequence(ResourceNodes);
        hash.AddSequence(Rods);
        hash.AddSequence(Bags);
        hash.AddSequence(Tombs);
        hash.AddSequence(Landmarks);
        hash.AddSequence(Mountains);
        hash.AddSequence(Sites);
        hash.AddSequence(Subraces);
        hash.Add(ScenarioState);
        hash.AddSequence(AiMemories);

        return hash.ToHashCode();
    }
}
