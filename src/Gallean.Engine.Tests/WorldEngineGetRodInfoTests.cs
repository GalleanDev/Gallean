namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetRodInfoTests
{
    private static UnitDefinition RodLeaderDefinition()
    {
        return TestHelpers.SimpleUnitDefinition("rod-leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Rod,
            LeaderAbilities = LeaderAbility.Rod,
            Leadership = 1,
            MovementPoints = 20
        };
    }

    [Test]
    public void PositionsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 4);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player.Id,
            new WorldPosition(0, 1),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        WorldGetRodInfoCommand.Result result = engine.CheckCommand(new WorldGetRodInfoCommand(stack.Id), []);

        Assert.That(result, Is.EqualTo(new WorldGetRodInfoCommand.Result(
            new WorldResourceSet(150, 0, 0, 0, 0, 0),
            [
                new WorldPosition(0, 0),
                new WorldPosition(1, 0),
                new WorldPosition(1, 1),
                new WorldPosition(0, 2),
                new WorldPosition(1, 2)
            ])));
    }

    [Test]
    public void UnaffordableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 4);

        UnitDefinition rodLeaderDefinition = RodLeaderDefinition();
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = WorldResourceSet.Empty
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("rod-leader"),
            player.Id,
            new WorldPosition(0, 1),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        WorldGetRodInfoCommand.Result result = engine.CheckCommand(new WorldGetRodInfoCommand(stack.Id), []);

        Assert.That(result, Is.EqualTo(new WorldGetRodInfoCommand.Result(new WorldResourceSet(150, 0, 0, 0, 0, 0), [])));
    }
}
