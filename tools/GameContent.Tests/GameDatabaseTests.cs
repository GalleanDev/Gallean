using Gallean.Engine.Contracts;
using Gallean.Entities;
using Gallean.Entities.GameData;

namespace GameContent.Tests;

public sealed class GameDatabaseTests
{
    [Test]
    public void BriefingVideosTest()
    {
        GameDatabase database = new(
            GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath),
            CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath));

        Assert.That(database.BriefingVideos[new ScenarioId("S100SC0000")], Is.EqualTo("BRIEFL01"));
        Assert.That(database.BriefingVideos[new ScenarioId("S107SC0000")], Is.EqualTo("BRIEFH01"));
        Assert.That(database.BriefingVideos[new ScenarioId("S126SC0000")], Is.EqualTo("BRIEF2L"));
        Assert.That(database.BriefingVideos[new ScenarioId("S135SC0000")], Is.EqualTo("BRIEF3EL"));
        Assert.That(database.BriefingVideos[new ScenarioId("S200SC0000")], Is.EqualTo("DEFAULT"));
    }

    [Test]
    public void VictoryMusicTest()
    {
        GameDatabase database = new(
            GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath),
            CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath));

        Assert.That(database.VictoryMusic[new ScenarioId("S095SC0000")], Is.EqualTo("WINH03"));
        Assert.That(database.VictoryMusic[new ScenarioId("S141SC0000")], Is.EqualTo("WINFGEN"));
        Assert.That(database.VictoryMusic[new ScenarioId("S142SC0000")], Is.EqualTo("WINF08"));
        Assert.That(database.VictoryMusic[new ScenarioId("S200SC0000")], Is.EqualTo("GENBRIEF"));
    }
}
