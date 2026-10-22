using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineConstructBuildingTests
{
    [Test]
    public void SuccessTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId mageLevel1Id = new("Mage1");
        WorldPlayerBuildingDefinitionId mageLevel2Id = new("Mage2");
        WorldPlayerBuildingDefinitionId fighterLevel1Id = new("Fighter1");

        WorldPlayerBuildingDefinition mageLevel1 = new(
            Id: mageLevel1Id,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Mage,
            Level: 1,
            Cost: new WorldResourceSet(100, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayerBuildingDefinition mageLevel2 = new(
            Id: mageLevel2Id,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: mageLevel1Id,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Mage,
            Level: 2,
            Cost: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayerBuildingDefinition fighterLevel1 = new(
            Id: fighterLevel1Id,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Fighter,
            Level: 2,
            Cost: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Undead,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(1000, 0, 0, 0, 0, 0),
            Buildings: new WorldPlayerBuildings(
                [mageLevel1Id, mageLevel2Id, fighterLevel1Id],
                []),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageLevel1)
            .WithDefinition(mageLevel2)
            .WithDefinition(fighterLevel1)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(playerId),
            new WorldTurnStartedEvent(playerId),
            new WorldTurnResourcesGeneratedEvent(playerId, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        engine.CheckCommand(new WorldConstructBuildingCommand(playerId, mageLevel1Id), [
            new WorldBuildingConstructedEvent(playerId, mageLevel1Id)
        ]);

        engine.CheckCommandError(
            new WorldConstructBuildingCommand(playerId, mageLevel2Id),
            WorldCommandError.BuildingAlreadyConstructedThisTurn);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(playerId),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(playerId),
            new WorldTurnStartedEvent(playerId),
            new WorldTurnResourcesGeneratedEvent(playerId, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        engine.CheckCommand(new WorldConstructBuildingCommand(playerId, mageLevel2Id), [
            new WorldBuildingConstructedEvent(playerId, mageLevel2Id)
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(playerId),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(playerId),
            new WorldTurnStartedEvent(playerId),
            new WorldTurnResourcesGeneratedEvent(playerId, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        engine.CheckCommand(new WorldConstructBuildingCommand(playerId, fighterLevel1Id), [
            new WorldBuildingConstructedEvent(playerId, fighterLevel1Id)
        ]);

        WorldPlayer expectedPlayer = player with
        {
            Resources = new WorldResourceSet(800, 75, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings(
                [mageLevel1Id, mageLevel2Id, fighterLevel1Id],
                [mageLevel1Id, mageLevel2Id, fighterLevel1Id]),
            TurnActions = new WorldPlayerTurnActions { BuildingConstructed = true }
        };

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Players = engine.State.Players.SetItem(playerId, expectedPlayer)
        }));
    }

    [Test]
    public void BuildingNotFoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId buildingId = new("Building");

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Undead,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: WorldResourceSet.Empty,
            Buildings: new WorldPlayerBuildings([], []),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(
            new WorldConstructBuildingCommand(playerId, buildingId),
            WorldCommandError.BuildingNotFound);
    }

    [Test]
    public void AlreadyExistsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId buildingId = new("Building");

        WorldPlayerBuildingDefinition building = new(
            Id: buildingId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Mage,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Undead,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: WorldResourceSet.Empty,
            Buildings: new WorldPlayerBuildings([buildingId], [buildingId]),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(building)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(
            new WorldConstructBuildingCommand(playerId, buildingId),
            WorldCommandError.BuildingAlreadyExists);
    }

    [Test]
    public void BranchLockedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId builtBuildingId = new("BuiltBuilding");
        WorldPlayerBuildingDefinitionId buildingId = new("Building");

        WorldPlayerBuildingDefinition builtBuilding = new(
            Id: builtBuildingId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Mage,
            Level: 2,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayerBuildingDefinition building = new(
            Id: buildingId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Mage,
            Level: 2,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Undead,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: WorldResourceSet.Empty,
            Buildings: new WorldPlayerBuildings([builtBuildingId, buildingId], [builtBuildingId]),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(builtBuilding)
            .WithDefinition(building)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(
            new WorldConstructBuildingCommand(playerId, buildingId),
            WorldCommandError.BuildingBranchLocked);
    }

    [Test]
    public void SideshowBesideSpecialTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId specialBuildingId = new("SpecialBuilding");
        WorldPlayerBuildingDefinitionId sideshowBuildingId = new("SideshowBuilding");

        WorldPlayerBuildingDefinition specialBuilding = new(
            Id: specialBuildingId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Special,
            Level: 3,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayerBuildingDefinition sideshowBuilding = new(
            Id: sideshowBuildingId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Sideshow,
            Level: 3,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Undead,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: WorldResourceSet.Empty,
            Buildings: new WorldPlayerBuildings([specialBuildingId, sideshowBuildingId], [specialBuildingId]),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(specialBuilding)
            .WithDefinition(sideshowBuilding)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(new WorldConstructBuildingCommand(playerId, sideshowBuildingId), [
            new WorldBuildingConstructedEvent(playerId, sideshowBuildingId)
        ]);
    }

    [Test]
    public void InsufficientResourcesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId buildingId = new("Building");

        WorldPlayerBuildingDefinition building = new(
            Id: buildingId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Mage,
            Level: 1,
            Cost: new WorldResourceSet(100, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Undead,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: WorldResourceSet.Empty,
            Buildings: new WorldPlayerBuildings([buildingId], []),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(building)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(
            new WorldConstructBuildingCommand(playerId, buildingId),
            WorldCommandError.InsufficientResources);
    }

    [Test]
    public void MissingPrerequisiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId buildingId = new("Building");
        WorldPlayerBuildingDefinitionId prerequisiteId = new("Prerequisite");

        WorldPlayerBuildingDefinition prerequisite = new(
            Id: prerequisiteId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Mage,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayerBuildingDefinition building = new(
            Id: buildingId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: prerequisiteId,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Mage,
            Level: 2,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Undead,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: WorldResourceSet.Empty,
            Buildings: new WorldPlayerBuildings([buildingId], []),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(prerequisite)
            .WithDefinition(building)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(
            new WorldConstructBuildingCommand(playerId, buildingId),
            WorldCommandError.BuildingMissingPrerequisite);
    }

    [Test]
    public void BranchLockedPrerequisiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId builtBuildingId = new("BuiltBuilding");
        WorldPlayerBuildingDefinitionId prerequisiteId = new("Prerequisite");
        WorldPlayerBuildingDefinitionId buildingId = new("Building");

        WorldPlayerBuildingDefinition builtBuilding = new(
            Id: builtBuildingId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Fighter,
            Level: 2,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayerBuildingDefinition prerequisite = new(
            Id: prerequisiteId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Fighter,
            Level: 2,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayerBuildingDefinition building = new(
            Id: buildingId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: prerequisiteId,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Fighter,
            Level: 3,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Undead,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: WorldResourceSet.Empty,
            Buildings: new WorldPlayerBuildings([builtBuildingId, prerequisiteId, buildingId], [builtBuildingId]),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(builtBuilding)
            .WithDefinition(prerequisite)
            .WithDefinition(building)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(
            new WorldConstructBuildingCommand(playerId, buildingId),
            WorldCommandError.BuildingBranchLocked);
    }

    [Test]
    public void MissingPrerequisiteOrderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId prerequisiteId = new("Prerequisite");
        WorldPlayerBuildingDefinitionId buildingId = new("Building");

        WorldPlayerBuildingDefinition prerequisite = new(
            Id: prerequisiteId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Mage,
            Level: 1,
            Cost: new WorldResourceSet(100, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayerBuildingDefinition building = new(
            Id: buildingId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: prerequisiteId,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: UnitBranch.Mage,
            Level: 2,
            Cost: new WorldResourceSet(100, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Undead,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: WorldResourceSet.Empty,
            Buildings: new WorldPlayerBuildings([prerequisiteId, buildingId], []),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions { BuildingConstructed = true });

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(prerequisite)
            .WithDefinition(building)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(
            new WorldConstructBuildingCommand(playerId, buildingId),
            WorldCommandError.BuildingMissingPrerequisite);
    }
}
