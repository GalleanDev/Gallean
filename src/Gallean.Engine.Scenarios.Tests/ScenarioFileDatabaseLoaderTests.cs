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
}
