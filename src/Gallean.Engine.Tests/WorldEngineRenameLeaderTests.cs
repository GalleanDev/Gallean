namespace Gallean.Engine.Tests;

public sealed class WorldEngineRenameLeaderTests
{
    [Test]
    public void SimpleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition squireDefinition = TestHelpers.SimpleUnitDefinition("squire", maxHitpoints: 100, initiative: 40, damage: 10);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("squire"), squireDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(squireDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldRenameLeaderCommand(stack.Id, "  Arthas  "),
            [
                new WorldLeaderRenamedEvent(stack.Id, new WorldUnitId("leader"), new LiteralUnitName("Arthas"))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Name = new LiteralUnitName("Arthas") }, GridPosition.BackBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("squire"), squireDefinition), GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void ValidationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 6);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            enemy.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemyLeader"), leaderDefinition), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(enemyStack)
            .Build();

        engine.CheckCommandError(new WorldRenameLeaderCommand(new WorldStackId("missing"), "Arthas"), WorldCommandError.StackNotFound);
        engine.CheckCommandError(new WorldRenameLeaderCommand(enemyStack.Id, "Arthas"), WorldCommandError.StackNotFound);
        engine.CheckCommandError(new WorldRenameLeaderCommand(stack.Id, "   "), WorldCommandError.LeaderNameEmpty);
        engine.CheckCommandError(new WorldRenameLeaderCommand(stack.Id, "Arthas the Unyielding"), WorldCommandError.LeaderNameTooLong);
    }
}
