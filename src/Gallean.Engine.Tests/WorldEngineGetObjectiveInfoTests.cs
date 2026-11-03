namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetObjectiveInfoTests
{
    [Test]
    public void LandObjectiveTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "HH..",
            "^~.."
        ]);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventTransformingLandCondition(50)],
            Effects: [new ScenarioEventWinEffect(player.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldGetObjectiveInfoCommand.Result result = engine.CheckCommand(new WorldGetObjectiveInfoCommand(), []);

        Assert.That(result, Is.EqualTo(new WorldGetObjectiveInfoCommand.Result(new WorldGetObjectiveInfoCommand.LandInfo(33, 50))));
    }

    [Test]
    public void DisabledLandObjectiveTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "HH..",
            "^~.."
        ]);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventTransformingLandCondition(50)],
            Effects: [new ScenarioEventWinEffect(player.Id)],
            Enabled: false,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldGetObjectiveInfoCommand.Result result = engine.CheckCommand(new WorldGetObjectiveInfoCommand(), []);

        Assert.That(result, Is.EqualTo(new WorldGetObjectiveInfoCommand.Result(Land: null)));
    }
}
