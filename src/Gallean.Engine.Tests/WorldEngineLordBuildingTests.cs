using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineLordBuildingTests
{
    [Test]
    public void GuildmasterTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId guildId = new("Guild");
        WorldPlayerBuildingDefinitionId magicId = new("Magic");
        WorldPlayerBuildingDefinitionId templeId = new("Temple");

        WorldPlayerBuildingDefinition guild = new(
            Id: guildId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Guild,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(150, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayerBuildingDefinition magic = new(
            Id: magicId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Spell,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(150, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayerBuildingDefinition temple = new(
            Id: templeId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Human,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(1000, 0, 0, 0, 0, 0),
            Buildings: new WorldPlayerBuildings([guildId, magicId, templeId], []),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(guild)
            .WithDefinition(magic)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(playerId),
            new WorldTurnStartedEvent(playerId),
            new WorldTurnResourcesGeneratedEvent(playerId, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State.Players[playerId], Is.EqualTo(new WorldPlayer(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Human,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(1100, 0, 0, 25, 0, 0),
            Buildings: new WorldPlayerBuildings([guildId, magicId, templeId], [guildId]),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(1, 1, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions())));
    }

    [Test]
    public void MageTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId guildId = new("Guild");
        WorldPlayerBuildingDefinitionId magicId = new("Magic");
        WorldPlayerBuildingDefinitionId templeId = new("Temple");

        WorldPlayerBuildingDefinition guild = new(
            Id: guildId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Guild,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(150, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayerBuildingDefinition magic = new(
            Id: magicId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Spell,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(150, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayerBuildingDefinition temple = new(
            Id: templeId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Human,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(1000, 0, 0, 0, 0, 0),
            Buildings: new WorldPlayerBuildings([guildId, magicId, templeId], []),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(guild)
            .WithDefinition(magic)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(playerId),
            new WorldTurnStartedEvent(playerId),
            new WorldTurnResourcesGeneratedEvent(playerId, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State.Players[playerId], Is.EqualTo(new WorldPlayer(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Human,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(1100, 0, 0, 25, 0, 0),
            Buildings: new WorldPlayerBuildings([guildId, magicId, templeId], [magicId]),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(1, 1, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions())));
    }

    [Test]
    public void WarriorTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId guildId = new("Guild");
        WorldPlayerBuildingDefinitionId magicId = new("Magic");
        WorldPlayerBuildingDefinitionId templeId = new("Temple");

        WorldPlayerBuildingDefinition guild = new(
            Id: guildId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Guild,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(150, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayerBuildingDefinition magic = new(
            Id: magicId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Spell,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(150, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayerBuildingDefinition temple = new(
            Id: templeId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Human,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(1000, 0, 0, 0, 0, 0),
            Buildings: new WorldPlayerBuildings([guildId, magicId, templeId], []),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(guild)
            .WithDefinition(magic)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(playerId),
            new WorldTurnStartedEvent(playerId),
            new WorldTurnResourcesGeneratedEvent(playerId, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State.Players[playerId], Is.EqualTo(new WorldPlayer(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Human,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(1100, 0, 0, 25, 0, 0),
            Buildings: new WorldPlayerBuildings([guildId, magicId, templeId], []),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(1, 1, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions())));
    }

    [Test]
    public void ConstructionStillAllowedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId guildId = new("Guild");
        WorldPlayerBuildingDefinitionId templeId = new("Temple");

        WorldPlayerBuildingDefinition guild = new(
            Id: guildId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Guild,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(150, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayerBuildingDefinition temple = new(
            Id: templeId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Human,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(1000, 0, 0, 0, 0, 0),
            Buildings: new WorldPlayerBuildings([guildId, templeId], []),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(guild)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(playerId),
            new WorldTurnStartedEvent(playerId),
            new WorldTurnResourcesGeneratedEvent(playerId, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        engine.CheckCommand(new WorldConstructBuildingCommand(playerId, templeId), [
            new WorldBuildingConstructedEvent(playerId, templeId)
        ]);

        Assert.That(engine.State.Players[playerId], Is.EqualTo(new WorldPlayer(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Human,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(800, 0, 0, 25, 0, 0),
            Buildings: new WorldPlayerBuildings([guildId, templeId], [guildId, templeId]),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(1, 1, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions { BuildingConstructed = true })));
    }

    [Test]
    public void NextTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId magicId = new("Magic");
        WorldPlayerBuildingDefinitionId templeId = new("Temple");

        WorldPlayerBuildingDefinition magic = new(
            Id: magicId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Spell,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(150, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayerBuildingDefinition temple = new(
            Id: templeId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Human,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(1000, 0, 0, 0, 0, 0),
            Buildings: new WorldPlayerBuildings([magicId, templeId], [templeId]),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(magic)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(playerId),
            new WorldTurnStartedEvent(playerId),
            new WorldTurnResourcesGeneratedEvent(playerId, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(playerId),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(playerId),
            new WorldTurnStartedEvent(playerId),
            new WorldTurnResourcesGeneratedEvent(playerId, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State.Players[playerId], Is.EqualTo(new WorldPlayer(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Human,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(1200, 0, 0, 50, 0, 0),
            Buildings: new WorldPlayerBuildings([magicId, templeId], [magicId, templeId]),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(1, 1, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions())));
    }

    [Test]
    public void NoAvailableBuildingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerId playerId = new(1);
        WorldPlayerBuildingDefinitionId templeId = new("Temple");

        WorldPlayerBuildingDefinition temple = new(
            Id: templeId,
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayer player = new(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Human,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(1000, 0, 0, 0, 0, 0),
            Buildings: new WorldPlayerBuildings([templeId], []),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(map.Width, map.Height, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(playerId),
            new WorldTurnStartedEvent(playerId),
            new WorldTurnResourcesGeneratedEvent(playerId, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State.Players[playerId], Is.EqualTo(new WorldPlayer(
            Id: playerId,
            NameTextId: new ScenarioTextId("Name"),
            LordId: new LordDefinitionId("Lord1"),
            Race: WorldRace.Human,
            FaceVariant: new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            Resources: new WorldResourceSet(1100, 0, 0, 25, 0, 0),
            Buildings: new WorldPlayerBuildings([templeId], []),
            Spellbook: new WorldPlayerSpellbook([], []),
            FogOfWar: ImmutableBitmap.Create(1, 1, true),
            SpiedPlayers: [],
            SpiedRuins: [],
            IncomingDiplomacyProposals: [],
            TurnActions: new WorldPlayerTurnActions())));
    }
}
