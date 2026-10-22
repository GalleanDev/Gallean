using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldStateTests
{
    [Test]
    public void EqualityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = new(
            Id: new WorldPlayerId(1),
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            AiAttitude: WorldAiAttitudeType.Medium,
            Race: WorldRace.Undead,
            FaceVariant: new WorldObjectVariant(0),
            IsAi: false,
            Resources: WorldResourceSet.Empty,
            Buildings: new WorldPlayerBuildings(
                [new WorldPlayerBuildingDefinitionId("available")],
                [new WorldPlayerBuildingDefinitionId("built")]),
            Spellbook: new WorldPlayerSpellbook(
                [new WorldSpellDefinitionId("available")],
                [new WorldSpellDefinitionId("known")]),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions
            {
                SpellResearched = true,
                SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty
                    .Add(new WorldSpellDefinitionId("cast"), 1)
            }
        );
        WorldState state = new(
            Map: map,
            TurnInfo: new WorldTurnInfo(1, new WorldPlayerId(1)),
            Difficulty: WorldDifficulty.Average,
            Players: ImmutableSortedDictionary<WorldPlayerId, WorldPlayer>.Empty.Add(player.Id, player),
            Relations: ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            Stacks: ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
            Fortifications: ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            ResourceNodes: ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            Rods: ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            Bags: ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            Tombs: ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty,
            Landmarks: ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
            Mountains: ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            Sites: ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            Subraces: ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            ScenarioState: WorldScenarioState.Empty,
            AiMemories: ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty
        );
        WorldPlayer equalPlayer = player with
        {
            Buildings = new WorldPlayerBuildings(
                [new WorldPlayerBuildingDefinitionId("available")],
                [new WorldPlayerBuildingDefinitionId("built")]),
            Spellbook = new WorldPlayerSpellbook(
                [new WorldSpellDefinitionId("available")],
                [new WorldSpellDefinitionId("known")]),
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height, true),
            TurnActions = new WorldPlayerTurnActions
            {
                SpellResearched = true,
                SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty
                    .Add(new WorldSpellDefinitionId("cast"), 1)
            }
        };
        WorldState equalState = state with
        {
            Map = TestHelpers.SimpleMap(1, 1),
            Players = ImmutableSortedDictionary<WorldPlayerId, WorldPlayer>.Empty.Add(equalPlayer.Id, equalPlayer)
        };

        Assert.That(equalState.Players, Is.Not.SameAs(state.Players));
        Assert.That(equalState, Is.EqualTo(state));
        Assert.That(equalState.GetHashCode(), Is.EqualTo(state.GetHashCode()));

        WorldState differentState = equalState with
        {
            TurnInfo = new WorldTurnInfo(2, new WorldPlayerId(1))
        };

        Assert.That(differentState, Is.Not.EqualTo(state));
    }
}
