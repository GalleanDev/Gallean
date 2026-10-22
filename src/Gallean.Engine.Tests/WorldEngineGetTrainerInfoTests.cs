namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetTrainerInfoTests
{
    [Test]
    public void UnitsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
        };
        UnitDefinition guardianDefinition = TestHelpers.SimpleUnitDefinition("guardian", maxHitpoints: 100, initiative: 30, damage: 10);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(200, 0, 0, 0, 0, 0)
        };

        WorldTrainerSite site = new(
            Id: new WorldSiteId("site"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Experience = 900 }, GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guardian"), guardianDefinition), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(guardianDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetTrainerInfoCommand.Result result = engine.CheckCommand(
            new WorldGetTrainerInfoCommand(stack.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetTrainerInfoCommand.Result(
        [
            new WorldGetTrainerInfoCommand.TrainingUnit(
                new WorldUnitId("leader"),
                CurrentExperience: 0,
                MaxExperience: 999,
                AffordableExperience: 0,
                WorldResourceSet.Empty,
                WorldCommandError.UnitTrainingForbidden),
            new WorldGetTrainerInfoCommand.TrainingUnit(
                new WorldUnitId("soldier"),
                CurrentExperience: 900,
                MaxExperience: 999,
                AffordableExperience: 40,
                new WorldResourceSet(5, 0, 0, 0, 0, 0),
                Error: null),
            new WorldGetTrainerInfoCommand.TrainingUnit(
                new WorldUnitId("guardian"),
                CurrentExperience: 0,
                MaxExperience: 999,
                AffordableExperience: 0,
                WorldResourceSet.Empty,
                WorldCommandError.UnitTrainingForbidden)
        ])));
    }
}
