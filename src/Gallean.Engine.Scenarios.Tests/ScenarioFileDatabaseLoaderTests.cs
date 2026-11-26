using Gallean.Common;
using Gallean.Engine;
using Gallean.Entities.GameData;

namespace Gallean.Engine.Scenarios.Tests;

public sealed class ScenarioFileDatabaseLoaderTests
{
    private static IReadOnlyList<string> FormatRaceLeaders(WorldState state, IGameDefinitionProvider definitions)
    {
        List<string> rows = [];

        foreach (WorldStack stack in state.Stacks.Values)
        {
            WorldPlayer owner = state.Players[stack.OwnerId];

            if (owner.Race != WorldRace.Neutral)
            {
                (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(definitions);
                rows.Add($"{stack.Id} {owner.Race} {leader.DefinitionId} level {leader.Level} hp {leader.Hitpoints} xp {leader.Experience} upgrades {stack.PendingLeaderUpgrades}");
            }
        }

        return rows;
    }

    [Test]
    public void LoadExportTest()
    {
        using FileStream stream = File.OpenRead(TestScenarios.GreywaterCrossingPath);

        ScenarioFile file = ScenarioFileLoader.Load(stream, TestScenarios.Encoding, new Db.ScenarioId("S200SC0000"));

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioFileDatabaseLoader.Load([file.Tables]);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        WorldState state = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables()).State;

        Assert.That(state.Map.Width, Is.EqualTo(48));
        Assert.That(state.Fortifications.Keys, Is.EqualTo([
            new WorldFortificationId("S200FT0000"),
            new WorldFortificationId("S200FT0001"),
            new WorldFortificationId("S200FT0002"),
            new WorldFortificationId("S200FT0003"),
            new WorldFortificationId("S200FT0004"),
            new WorldFortificationId("S200RU0000"),
            new WorldFortificationId("S200RU0001")
        ]));
        Assert.That(state.Stacks.Keys, Is.EqualTo([
            new WorldStackId("S200KC0000"),
            new WorldStackId("S200KC0001"),
            new WorldStackId("S200KC0002"),
            new WorldStackId("S200KC0003"),
            new WorldStackId("S200KC0004"),
            new WorldStackId("S200KC0005"),
            new WorldStackId("S200KC0006"),
            new WorldStackId("S200KC0007")
        ]));
        Assert.That(state.TurnInfo, Is.EqualTo(new WorldTurnInfo(1, new WorldPlayerId(1))));
        Assert.That(state.Rods.Values, Is.Empty);
    }

    [Test]
    public void LoadSaveTest()
    {
        using FileStream stream = File.OpenRead(TestScenarios.GreywaterSiegePath);

        ScenarioFile file = ScenarioFileLoader.Load(stream, TestScenarios.Encoding, new Db.ScenarioId("S200SC0000"));

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioFileDatabaseLoader.Load([file.Tables]);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        WorldState state = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables()).State;

        Assert.That(state.Map.Width, Is.EqualTo(48));
        Assert.That(state.Fortifications.Keys, Is.EqualTo([
            new WorldFortificationId("S200FT0000"),
            new WorldFortificationId("S200FT0001"),
            new WorldFortificationId("S200FT0002"),
            new WorldFortificationId("S200FT0003"),
            new WorldFortificationId("S200FT0004"),
            new WorldFortificationId("S200RU0000"),
            new WorldFortificationId("S200RU0001")
        ]));
        Assert.That(state.Stacks.Keys, Is.EqualTo([
            new WorldStackId("S200KC0000"),
            new WorldStackId("S200KC0001"),
            new WorldStackId("S200KC0002"),
            new WorldStackId("S200KC0003"),
            new WorldStackId("S200KC0004"),
            new WorldStackId("S200KC0005"),
            new WorldStackId("S200KC0006"),
            new WorldStackId("S200KC0007")
        ]));
        Assert.That(state.TurnInfo, Is.EqualTo(new WorldTurnInfo(3, new WorldPlayerId(1))));
        Assert.That(state.Rods.Values, Is.EqualTo([
            new WorldRod(new WorldRodId("S200RD0000"), new WorldPlayerId(2), new WorldPosition(10, 5))
        ]));
    }

    [Test]
    public void LoadUnknownLandmarkTest()
    {
        using FileStream stream = File.OpenRead(TestScenarios.GreywaterCrossingPath);

        ScenarioFile file = ScenarioFileLoader.Load(stream, TestScenarios.Encoding, new Db.ScenarioId("S200SC0000"));
        ScenarioFileTables tables = file.Tables with
        {
            ScenarioLandmarks = [.. file.Tables.ScenarioLandmarks.Select(landmark => landmark with { TypeId = new Db.Id("G000MG9999") })]
        };

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioFileDatabaseLoader.Load([tables]);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);

        GameDefinitionNotFoundException ex = Assert.Throws<GameDefinitionNotFoundException>(
            () => ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables()));

        Assert.That(ex.Id, Is.EqualTo("G000MG9999"));
        Assert.That(ex.Message, Is.EqualTo("Landmark definition 'G000MG9999' was not found."));
    }

    [Test]
    public void LoadUnknownUnitTest()
    {
        using FileStream stream = File.OpenRead(TestScenarios.GreywaterCrossingPath);

        ScenarioFile file = ScenarioFileLoader.Load(stream, TestScenarios.Encoding, new Db.ScenarioId("S200SC0000"));
        ScenarioFileTables tables = file.Tables with
        {
            ScenarioUnits = [.. file.Tables.ScenarioUnits.Select(unit => unit with { TypeId = new Db.Id("G000UU9999") })]
        };

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioFileDatabaseLoader.Load([tables]);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);

        GameDefinitionNotFoundException ex = Assert.Throws<GameDefinitionNotFoundException>(
            () => ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables()));

        Assert.That(ex.Id, Is.EqualTo("G000UU9999"));
        Assert.That(ex.Message, Is.EqualTo("Unit definition 'G000UU9999' was not found."));
    }

    [Test]
    public void LoadMissingOrderLocationTest()
    {
        using FileStream stream = File.OpenRead(TestScenarios.GreywaterCrossingPath);

        ScenarioFile file = ScenarioFileLoader.Load(stream, TestScenarios.Encoding, new Db.ScenarioId("S200SC0000"));
        ScenarioFileTables tables = file.Tables with
        {
            ScenarioStackTemplates =
            [
                .. file.Tables.ScenarioStackTemplates.Select(template => template with { OrderTargetId = new Db.Id("S200LO0099") })
            ]
        };

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioFileDatabaseLoader.Load([tables]);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables());

        UnitGrid<ScenarioStackTemplateUnit> expectedUnits = new UnitGrid<ScenarioStackTemplateUnit>.Builder()
            .Place(new ScenarioStackTemplateUnit(new UnitDefinitionId("G000UU0099"), 100, 1, [], new ScenarioUnitName(new ScenarioTextId("S200TM0001:NAME_TXT:0"))), GridPosition.FrontCenter, false)
            .Place(new ScenarioStackTemplateUnit(new UnitDefinitionId("G000UU0078"), 45, 1, [], null), GridPosition.BackBottom, false)
            .Build();

        Assert.That(scenario.Provider.GetStackTemplate(new ScenarioStackTemplateId("S200TM0001")), Is.EqualTo(
            new ScenarioStackTemplateDefinition(
                new ScenarioStackTemplateId("S200TM0001"),
                new WorldPlayerId(3),
                new WorldSubraceId("S200SR0002"),
                expectedUnits,
                WorldStackOrder.Stand.Instance,
                WorldStackDirection.East,
                AiPriority: 3)));
    }

    [Test]
    public void LoadMissingDefendOrderLocationTest()
    {
        using FileStream stream = File.OpenRead(TestScenarios.GreywaterCrossingPath);

        ScenarioFile file = ScenarioFileLoader.Load(stream, TestScenarios.Encoding, new Db.ScenarioId("S200SC0000"));
        ScenarioFileTables tables = file.Tables with
        {
            ScenarioStackTemplates =
            [
                .. file.Tables.ScenarioStackTemplates.Select(template => template with { Order = Db.OrderType.DefendLocation, OrderTargetId = new Db.Id("S200LO0099") })
            ]
        };

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioFileDatabaseLoader.Load([tables]);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables());

        UnitGrid<ScenarioStackTemplateUnit> expectedUnits = new UnitGrid<ScenarioStackTemplateUnit>.Builder()
            .Place(new ScenarioStackTemplateUnit(new UnitDefinitionId("G000UU0099"), 100, 1, [], new ScenarioUnitName(new ScenarioTextId("S200TM0001:NAME_TXT:0"))), GridPosition.FrontCenter, false)
            .Place(new ScenarioStackTemplateUnit(new UnitDefinitionId("G000UU0078"), 45, 1, [], null), GridPosition.BackBottom, false)
            .Build();

        Assert.That(scenario.Provider.GetStackTemplate(new ScenarioStackTemplateId("S200TM0001")), Is.EqualTo(
            new ScenarioStackTemplateDefinition(
                new ScenarioStackTemplateId("S200TM0001"),
                new WorldPlayerId(3),
                new WorldSubraceId("S200SR0002"),
                expectedUnits,
                WorldStackOrder.Stand.Instance,
                WorldStackDirection.East,
                AiPriority: 3)));
    }

    [Test]
    public void LoadMissingOrderCityTest()
    {
        using FileStream stream = File.OpenRead(TestScenarios.GreywaterCrossingPath);

        ScenarioFile file = ScenarioFileLoader.Load(stream, TestScenarios.Encoding, new Db.ScenarioId("S200SC0000"));
        ScenarioFileTables tables = file.Tables with
        {
            ScenarioStackTemplates =
            [
                .. file.Tables.ScenarioStackTemplates.Select(template => template with { Order = Db.OrderType.SecureCity, OrderTargetId = new Db.Id("S200FT0099") })
            ]
        };

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioFileDatabaseLoader.Load([tables]);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables());

        UnitGrid<ScenarioStackTemplateUnit> expectedUnits = new UnitGrid<ScenarioStackTemplateUnit>.Builder()
            .Place(new ScenarioStackTemplateUnit(new UnitDefinitionId("G000UU0099"), 100, 1, [], new ScenarioUnitName(new ScenarioTextId("S200TM0001:NAME_TXT:0"))), GridPosition.FrontCenter, false)
            .Place(new ScenarioStackTemplateUnit(new UnitDefinitionId("G000UU0078"), 45, 1, [], null), GridPosition.BackBottom, false)
            .Build();

        Assert.That(scenario.Provider.GetStackTemplate(new ScenarioStackTemplateId("S200TM0001")), Is.EqualTo(
            new ScenarioStackTemplateDefinition(
                new ScenarioStackTemplateId("S200TM0001"),
                new WorldPlayerId(3),
                new WorldSubraceId("S200SR0002"),
                expectedUnits,
                WorldStackOrder.Stand.Instance,
                WorldStackDirection.East,
                AiPriority: 3)));
    }

    [Test]
    public void LoadMissingOrderStackTest()
    {
        using FileStream stream = File.OpenRead(TestScenarios.GreywaterCrossingPath);

        ScenarioFile file = ScenarioFileLoader.Load(stream, TestScenarios.Encoding, new Db.ScenarioId("S200SC0000"));
        ScenarioFileTables tables = file.Tables with
        {
            ScenarioStackTemplates =
            [
                .. file.Tables.ScenarioStackTemplates.Select(template => template with { Order = Db.OrderType.AttackStack, OrderTargetId = new Db.Id("S200KC0099") })
            ]
        };

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioFileDatabaseLoader.Load([tables]);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables());

        UnitGrid<ScenarioStackTemplateUnit> expectedUnits = new UnitGrid<ScenarioStackTemplateUnit>.Builder()
            .Place(new ScenarioStackTemplateUnit(new UnitDefinitionId("G000UU0099"), 100, 1, [], new ScenarioUnitName(new ScenarioTextId("S200TM0001:NAME_TXT:0"))), GridPosition.FrontCenter, false)
            .Place(new ScenarioStackTemplateUnit(new UnitDefinitionId("G000UU0078"), 45, 1, [], null), GridPosition.BackBottom, false)
            .Build();

        Assert.That(scenario.Provider.GetStackTemplate(new ScenarioStackTemplateId("S200TM0001")), Is.EqualTo(
            new ScenarioStackTemplateDefinition(
                new ScenarioStackTemplateId("S200TM0001"),
                new WorldPlayerId(3),
                new WorldSubraceId("S200SR0002"),
                expectedUnits,
                new WorldStackOrder.AttackStack(new WorldStackId("S200KC0099")),
                WorldStackDirection.East,
                AiPriority: 3)));
    }

    [Test]
    public void LoadLeaderTest()
    {
        using FileStream stream = File.OpenRead(TestScenarios.GreywaterCrossingPath);

        ScenarioFile file = ScenarioFileLoader.Load(stream, TestScenarios.Encoding, new Db.ScenarioId("S200SC0000"));

        ScenarioLeader leader = new(
            new WorldUnit(new WorldUnitId("Hero"), new UnitDefinitionId("G000UU0096"), Hitpoints: 150, Experience: 0, Level: 10, [], Name: null, IsTypeLocked: false),
            LeaderEquipment.Empty,
            WorldInventory.Empty,
            PendingLeaderUpgrades: 0,
            BattlesWon: 0);
        ScenarioConfiguration configuration = new([new ScenarioPlayerConfiguration(WorldRace.Undead, LordCategory.Warrior, new WorldObjectVariant(1), null, LeaderName: "")], WorldDifficulty.Hard, leader);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioFileDatabaseLoader.Load([file.Tables]);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        Scenario scenario = ScenarioLoader.Create(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables(), configuration);

        Assert.That(FormatRaceLeaders(scenario.State, definitions), Is.EqualTo([
            "S200KC0000 Dwarf G000UU0044 level 3 hp 255 xp 0 upgrades 2",
            "S200KC0001 Undead G000UU0096 level 10 hp 285 xp 0 upgrades 0",
            "S200KC0005 Dwarf G000UU0047 level 2 hp 150 xp 40 upgrades 1",
            "S200KC0006 Undead G000UU0097 level 1 hp 90 xp 0 upgrades 0"
        ]));
    }

    [Test]
    public void LoadMissingCapitalStackTest()
    {
        using FileStream stream = File.OpenRead(TestScenarios.GreywaterCrossingPath);

        ScenarioFile file = ScenarioFileLoader.Load(stream, TestScenarios.Encoding, new Db.ScenarioId("S200SC0000"));
        ScenarioFileTables tables = file.Tables with
        {
            ScenarioStacks =
            [
                .. file.Tables.ScenarioStacks.Where(stack => stack.StackId != new Db.Id("S200KC0000"))
            ]
        };

        ScenarioLeader leader = new(
            new WorldUnit(new WorldUnitId("Hero"), new UnitDefinitionId("G000UU0096"), Hitpoints: 150, Experience: 0, Level: 10, [], Name: null, IsTypeLocked: false),
            LeaderEquipment.Empty,
            WorldInventory.Empty,
            PendingLeaderUpgrades: 0,
            BattlesWon: 0);
        ScenarioConfiguration configuration = new([new ScenarioPlayerConfiguration(WorldRace.Undead, LordCategory.Warrior, new WorldObjectVariant(1), null, LeaderName: "")], WorldDifficulty.Hard, leader);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioFileDatabaseLoader.Load([tables]);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        Scenario scenario = ScenarioLoader.Create(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables(), configuration);

        Assert.That(FormatRaceLeaders(scenario.State, definitions), Is.EqualTo([
            "S200KC0001 Undead G000UU0096 level 10 hp 285 xp 0 upgrades 0",
            "S200KC0005 Dwarf G000UU0047 level 2 hp 150 xp 40 upgrades 1",
            "S200KC0006 Undead G000UU0097 level 1 hp 90 xp 0 upgrades 0"
        ]));
    }
}
