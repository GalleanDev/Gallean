using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetMoveInfoTests
{
    private static string[] FormatReachablePositions(WorldMap map, WorldStack stack, WorldGetMoveInfoCommand.Result result)
    {
        HashSet<WorldPosition> reachable = [.. result.ReachablePositions];
        string[] rows = new string[map.Height];

        for (int y = 0; y < map.Height; y++)
        {
            char[] row = new char[map.Width];

            for (int x = 0; x < map.Width; x++)
            {
                WorldPosition position = new(x, y);

                if (position == stack.Position)
                {
                    row[x] = 'S';
                }
                else if (reachable.Contains(position))
                {
                    row[x] = '*';
                }
                else
                {
                    row[x] = '.';
                }
            }

            rows[y] = new string(row);
        }

        return rows;
    }

    [Test]
    public void SimpleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 4, leadership: 6) with
            {
                VisionRadius = 8
            };
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMoveInfoCommand.Result result = engine.CheckCommand(new WorldGetMoveInfoCommand(stack.Id), []);

        Assert.That(FormatReachablePositions(map, stack, result), Is.EqualTo([
            ".......",
            ".*****.",
            ".*****.",
            ".**S**.",
            ".*****.",
            ".*****.",
            "......."
        ]));
    }

    [Test]
    public void NoMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 0, leadership: 6) with
            {
                VisionRadius = 8
            };
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMoveInfoCommand.Result result = engine.CheckCommand(new WorldGetMoveInfoCommand(stack.Id), []);

        Assert.That(result, Is.EqualTo(new WorldGetMoveInfoCommand.Result([])));
    }

    [Test]
    public void FogTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height)
        };
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 12, leadership: 6) with
            {
                VisionRadius = 2
            };
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 12);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMoveInfoCommand.Result result = engine.CheckCommand(new WorldGetMoveInfoCommand(stack.Id), []);

        Assert.That(FormatReachablePositions(map, stack, result), Is.EqualTo([
            "S**...."
        ]));
    }

    [Test]
    public void StacksTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                VisionRadius = 8
            };
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        WorldStack friendlyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);
        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack3"),
            enemyPlayer.Id,
            new WorldPosition(3, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithStack(stack)
            .WithStack(friendlyStack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMoveInfoCommand.Result result = engine.CheckCommand(new WorldGetMoveInfoCommand(stack.Id), []);

        Assert.That(FormatReachablePositions(map, stack, result), Is.EqualTo([
            "*.**..",
            "S***..",
            "****.."
        ]));
    }

    [Test]
    public void RiotingCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                VisionRadius = 8
            };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                RiotTurns = 3
            };
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(6, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMoveInfoCommand.Result result = engine.CheckCommand(new WorldGetMoveInfoCommand(stack.Id), []);

        Assert.That(FormatReachablePositions(map, stack, result), Is.EqualTo([
            "....****",
            "....****",
            "....****",
            "....**S*",
            "...*****"
        ]));
    }

    [Test]
    public void ThiefTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition() with
        {
            MovementPoints = 4,
            VisionRadius = 8
        };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Heretic, 1),
            WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            enemyPlayer.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMoveInfoCommand.Result result = engine.CheckCommand(new WorldGetMoveInfoCommand(stack.Id), []);

        Assert.That(FormatReachablePositions(map, stack, result), Is.EqualTo([
            ".......",
            "....***",
            "....***",
            "...**S*",
            "...****"
        ]));
    }

    [Test]
    public void ThiefRiotingCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition() with
        {
            MovementPoints = 4,
            VisionRadius = 8
        };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Heretic, 1),
            WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                enemyPlayer.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                RiotTurns = 3
            };
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMoveInfoCommand.Result result = engine.CheckCommand(new WorldGetMoveInfoCommand(stack.Id), []);

        Assert.That(FormatReachablePositions(map, stack, result), Is.EqualTo([
            ".......",
            "....***",
            "....***",
            "....*S*",
            "...****"
        ]));
    }

    [Test]
    public void ThiefPastEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition() with
        {
            MovementPoints = 10,
            VisionRadius = 8
        };
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 10);
        WorldStack friendlyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player.Id,
            new WorldPosition(4, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);
        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack3"),
            enemyPlayer.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithStack(stack)
            .WithStack(friendlyStack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMoveInfoCommand.Result result = engine.CheckCommand(new WorldGetMoveInfoCommand(stack.Id), []);

        Assert.That(FormatReachablePositions(map, stack, result), Is.EqualTo([
            "******.",
            "S***.*.",
            "******."
        ]));
    }

    [Test]
    public void InvalidStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldGetMoveInfoCommand(new WorldStackId("Missing")),
            WorldCommandError.StackNotFound);
    }
}
