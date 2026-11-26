using Gallean.Common;
using Gallean.Engine;
using Gallean.Entities.GameData;

namespace Gallean.Engine.Scenarios.Tests;

public sealed class ScenarioFileDatabaseLoaderTests
{
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
}
