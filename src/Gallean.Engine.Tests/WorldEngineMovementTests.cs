using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineMovementTests
{
    [Test]
    public void MoveStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(StackId: new WorldStackId("Stack1"), SourcePosition: new WorldPosition(0, 0), TargetPosition: new WorldPosition(0, 1), NewDirection: WorldStackDirection.SouthWest, NewMovementPoints: 4, RemainingPath: [], null),
                new WorldStackMovedEvent(StackId: new WorldStackId("Stack1"), SourcePosition: new WorldPosition(0, 0), TargetPosition: new WorldPosition(0, 1), NewDirection: WorldStackDirection.SouthWest, NewMovementPoints: 4, RemainingPath: [], null)
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(0, 1),
                    Direction = WorldStackDirection.SouthWest,
                    MovementPoints = 4
                })
            }));
    }

    [Test]
    public void MoveStackDeadLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 10, initiative: 10, damage: 10);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 0 }, GridPosition.FrontBottom, leader.IsLarge)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontCenter, soldier.IsLarge)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(StackId: new WorldStackId("Stack1"), SourcePosition: new WorldPosition(0, 0), TargetPosition: new WorldPosition(0, 1), NewDirection: WorldStackDirection.SouthWest, NewMovementPoints: 2, RemainingPath: [], null),
                new WorldStackMovedEvent(StackId: new WorldStackId("Stack1"), SourcePosition: new WorldPosition(0, 0), TargetPosition: new WorldPosition(0, 1), NewDirection: WorldStackDirection.SouthWest, NewMovementPoints: 2, RemainingPath: [], null)
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(0, 1),
                    Direction = WorldStackDirection.SouthWest,
                    MovementPoints = 2
                })
            }));
    }

    [Test]
    public void MoveStackDiagonallyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(2, 2)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(1, 1), new WorldPosition(2, 2), WorldStackDirection.South, 0, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(1, 1), new WorldPosition(2, 2), WorldStackDirection.South, 0, [], null)
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(2, 2),
                    Direction = WorldStackDirection.South,
                    MovementPoints = 0
                })
            }));
    }

    [Test]
    public void MoveStackExhaustedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(1, 0)),
            [
                new WorldStackMovingEvent(StackId: new WorldStackId("Stack1"), SourcePosition: new WorldPosition(0, 0), TargetPosition: new WorldPosition(1, 0), NewDirection: WorldStackDirection.SouthEast, NewMovementPoints: 0, RemainingPath: [], null),
                new WorldStackMovedEvent(StackId: new WorldStackId("Stack1"), SourcePosition: new WorldPosition(0, 0), TargetPosition: new WorldPosition(1, 0), NewDirection: WorldStackDirection.SouthEast, NewMovementPoints: 0, RemainingPath: [], null)
            ]
        );

        engine.CheckCommandError(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(2, 0)),
            WorldCommandError.InsufficientMovement
        );
    }

    [Test]
    public void MoveStackFogOfWarTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".....",
            ".~...",
            ".....",
            "....."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Unit_s1", maxHitpoints: 10, initiative: 50, damage: 5),
                movementPoints: 5, leadership: 6) with
            {
                VisionRadius = 3
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height, false)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 5);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        _ = engine.DispatchCommand(new WorldMoveStackCommand(new WorldStackId("s1"), new WorldPosition(0, 1)));

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[new WorldPlayerId(1)].FogOfWar), Is.EqualTo([
            "....?",
            "....?",
            "....?",
            "....?"
        ]));
    }

    [Test]
    public void MoveStackMultiTileTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5),
                movementPoints: 4, leadership: 6) with
            {
                VisionRadius = 3
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(0, 2)),
            [
                new WorldStackMovingEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 2, [new WorldPosition(0, 2)], null),
                new WorldStackMovedEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 2, [new WorldPosition(0, 2)], null),
                new WorldStackMovingEvent(new WorldStackId("Stack1"), new WorldPosition(0, 1), new WorldPosition(0, 2), WorldStackDirection.SouthWest, 0, [], null),
                new WorldStackMovedEvent(new WorldStackId("Stack1"), new WorldPosition(0, 1), new WorldPosition(0, 2), WorldStackDirection.SouthWest, 0, [], null)
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(0, 2),
                    Direction = WorldStackDirection.SouthWest,
                    MovementPoints = 0
                })
            }));
    }

    [Test]
    public void MoveStackOutOfBoundsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(0, 1)),
            WorldCommandError.TargetOutOfBounds
        );
    }

    [Test]
    public void MoveStackPathfindingTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^.",
            "...",
            "..."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5),
                movementPoints: 4, leadership: 6) with
            {
                VisionRadius = 3
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(2, 0)),
            [
                new WorldStackMovingEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(1, 1), WorldStackDirection.South, 2, [new WorldPosition(2, 0)], null),
                new WorldStackMovedEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(1, 1), WorldStackDirection.South, 2, [new WorldPosition(2, 0)], null),
                new WorldStackMovingEvent(new WorldStackId("Stack1"), new WorldPosition(1, 1), new WorldPosition(2, 0), WorldStackDirection.East, 0, [], null),
                new WorldStackMovedEvent(new WorldStackId("Stack1"), new WorldPosition(1, 1), new WorldPosition(2, 0), WorldStackDirection.East, 0, [], null)
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(2, 0),
                    Direction = WorldStackDirection.East,
                    MovementPoints = 0
                })
            }));
    }

    [Test]
    public void MoveOntoAdjacentEnemyCityTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^...",
            "^^^...",
            "^^^..."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition), GridPosition.FrontBottom, unitDefinition.IsLarge)
                .Build(),
            movementPoints: 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty) with
        {
            VisionRadius = 3
        };
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            new WorldPlayerId(2),
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit2"), unitDefinition), GridPosition.FrontBottom, unitDefinition.IsLarge)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Attacker, hitpoints: 10), GridPosition.FrontBottom)
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit2"), unitDefinition.Id, BattleSide.Defender, hitpoints: 10), GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, city.Entrance), [
            new WorldStackMovementConsumedEvent(stack.Id, 3),
            new WorldFortificationBattleStartedEvent(stack.Id, city.Id, city.Entrance, expectedBattleState)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Direction = WorldStackDirection.NorthWest,
                    MovementPoints = 3
                })
            }));
    }

    [Test]
    public void MoveStackTargetOccupiedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            new WorldPlayerId(1),
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldStack stack3 = TestHelpers.SimpleStack(
            new WorldStackId("Stack3"),
            new WorldPlayerId(1),
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(stack1)
            .WithStack(stack2)
            .WithStack(stack3)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(1, 0)),
            WorldCommandError.TargetOccupied
        );

        engine.CheckCommandError(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(0, 1)),
            WorldCommandError.TargetOccupied
        );
    }

    [Test]
    public void MoveStackBeyondMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5),
                movementPoints: 1, leadership: 6) with
            {
                VisionRadius = 3
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(0, 2)),
            [
                new WorldStackMovingEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 0, [], null),
                new WorldStackMovedEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 0, [], null)
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(0, 1),
                    Direction = WorldStackDirection.SouthWest,
                    MovementPoints = 0
                })
            }));
    }

    [Test]
    public void MoveStackUnpassableTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^.",
            "...",
            "..."
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(1, 0)),
            WorldCommandError.TargetUnpassable
        );
    }

    [Test]
    public void MoveStackWaterOnlyTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "~~~~",
            "~~~~",
            "~~~."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5),
                movementPoints: 4, leadership: 6) with
            {
                IsWaterOnly = true
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(3, 2)),
            WorldCommandError.TargetUnpassable
        );

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(2, 0)),
            [
                new WorldStackMovingEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 2, [new WorldPosition(2, 0)], null),
                new WorldStackMovedEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 2, [new WorldPosition(2, 0)], null),
                new WorldStackMovingEvent(new WorldStackId("Stack1"), new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 0, [], null),
                new WorldStackMovedEvent(new WorldStackId("Stack1"), new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 0, [], null)
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(2, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 0
                })
            }));
    }

    [Test]
    public void GetPathTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5),
                movementPoints: 1, leadership: 6) with
            {
                VisionRadius = 3
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(new WorldStackId("Stack1"), new WorldPosition(0, 2)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(0, 0), new WorldPosition(0, 1), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(0, 1), new WorldPosition(0, 2), 2, Turn: 1)
            ]),
            new WorldPath([]),
            null)));
    }

    [Test]
    public void GetPathNoPathTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^.",
            ".^.",
            ".^."
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldGetPathCommand(new WorldStackId("Stack1"), new WorldPosition(2, 1)),
            WorldCommandError.PathNotFound);
    }

    [Test]
    public void GetPathToEnemySpareMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            enemyPlayer.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithStack(stack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, enemyStack.Position),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
                [new WorldPathSegment(new WorldPosition(0, 0), new WorldPosition(1, 0), 2, Turn: 0)]),
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(1, 0), new WorldPosition(2, 0), 2, Turn: 0)
            ]),
            new WorldPathEncounter.Battle(new WorldPosition(2, 0)))));
    }

    [Test]
    public void GetPathToEnemyExactMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            enemyPlayer.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithStack(stack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, enemyStack.Position),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
                [new WorldPathSegment(new WorldPosition(0, 0), new WorldPosition(1, 0), 2, Turn: 0)]),
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(1, 0), new WorldPosition(2, 0), 2, Turn: 1)
            ]),
            new WorldPathEncounter.Battle(new WorldPosition(2, 0)))));
    }

    [Test]
    public void GetPathToEnemyCityTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("Unit1", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty) with
        {
            VisionRadius = 3
        };
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            enemyPlayer.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition), GridPosition.FrontBottom, unitDefinition.IsLarge)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, city.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
                [new WorldPathSegment(new WorldPosition(5, 3), new WorldPosition(4, 3), 2, Turn: 0)]),
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(4, 3), new WorldPosition(3, 3), 0, Turn: 0)
            ]),
            new WorldPathEncounter.Battle(city.Entrance))));
    }

    [Test]
    public void GetPathToEnemyCapitalTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("Unit1", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(6, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital1"),
            enemyPlayer.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition), GridPosition.FrontBottom, unitDefinition.IsLarge)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithStack(stack)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, capital.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
                [new WorldPathSegment(new WorldPosition(6, 4), new WorldPosition(5, 4), 2, Turn: 0)]),
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(5, 4), new WorldPosition(4, 4), 0, Turn: 0)
            ]),
            new WorldPathEncounter.Battle(capital.Entrance))));
    }

    [Test]
    public void NoPathTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^.",
            ".^.",
            ".^."
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(2, 1)),
            WorldCommandError.PathNotFound
        );
    }

    [Test]
    public void FortificationBlocksMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(2, 2)),
            WorldCommandError.PathNotFound);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, capital.Entrance),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 5), capital.Entrance, WorldStackDirection.North, 10, [], null),
                new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("capital")),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 5), capital.Entrance, WorldStackDirection.North, 10, [], null)
            ]);
    }

    [Test]
    public void RuinFootprintBlocksMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("ruin"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(4, 4), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Resources: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3
        );

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRuin(ruin)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(2, 2)),
            WorldCommandError.PathNotFound);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, ruin.Entrance),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 5), ruin.Entrance, WorldStackDirection.North, 8, [], null),
                new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("ruin")),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 5), ruin.Entrance, WorldStackDirection.North, 8, [], null)
            ]);
    }

    [Test]
    public void MoveAroundSiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldTrainerSite site = new(
            Id: new WorldSiteId("site"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.SiteFootprint),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3
        );

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(3, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 3)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(3, 1), new WorldPosition(3, 2), WorldStackDirection.SouthWest, 8, [new WorldPosition(2, 3), new WorldPosition(1, 3)], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(3, 1), new WorldPosition(3, 2), WorldStackDirection.SouthWest, 8, [new WorldPosition(2, 3), new WorldPosition(1, 3)], null),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(3, 2), new WorldPosition(2, 3), WorldStackDirection.West, 6, [new WorldPosition(1, 3)], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(3, 2), new WorldPosition(2, 3), WorldStackDirection.West, 6, [new WorldPosition(1, 3)], null),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(2, 3), new WorldPosition(1, 3), WorldStackDirection.NorthWest, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(2, 3), new WorldPosition(1, 3), WorldStackDirection.NorthWest, 4, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(1, 3),
                    Direction = WorldStackDirection.NorthWest,
                    MovementPoints = 4
                })
            }));
    }

    [Test]
    public void MoveOntoSiteEntranceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldTrainerSite site = new(
            Id: new WorldSiteId("site"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.SiteFootprint),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3
        );

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(3, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 1)),
            WorldCommandError.PathNotFound);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, site.Entrance),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(3, 1), site.Entrance, WorldStackDirection.West, 8, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(3, 1), site.Entrance, WorldStackDirection.West, 8, [], null),
                new WorldSiteVisitedEvent(stack.Id, site.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = site.Entrance,
                    Direction = WorldStackDirection.West,
                    MovementPoints = 8
                }),
                Sites = ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty.Add(site.Id, site with { VisitorIds = [player.Id] })
            }));
    }

    [Test]
    public void MoveAroundDefendedRuinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("ruin"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            Resources: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3
        );

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(3, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRuin(ruin)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 3)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(3, 1), new WorldPosition(3, 2), WorldStackDirection.SouthWest, 8, [new WorldPosition(2, 3), new WorldPosition(1, 3)], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(3, 1), new WorldPosition(3, 2), WorldStackDirection.SouthWest, 8, [new WorldPosition(2, 3), new WorldPosition(1, 3)], null),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(3, 2), new WorldPosition(2, 3), WorldStackDirection.West, 6, [new WorldPosition(1, 3)], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(3, 2), new WorldPosition(2, 3), WorldStackDirection.West, 6, [new WorldPosition(1, 3)], null),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(2, 3), new WorldPosition(1, 3), WorldStackDirection.NorthWest, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(2, 3), new WorldPosition(1, 3), WorldStackDirection.NorthWest, 4, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(1, 3),
                    Direction = WorldStackDirection.NorthWest,
                    MovementPoints = 4
                })
            }));
    }

    [Test]
    public void FootprintWallNoBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 10, leadership: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty) with
        {
            VisionRadius = 3
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition), GridPosition.FrontBottom, unitDefinition.IsLarge)
                .Build(),
            movementPoints: 10);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit2"), unitDefinition), GridPosition.FrontBottom, unitDefinition.IsLarge)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 4)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 4), new WorldPosition(1, 4), WorldStackDirection.SouthEast, 8, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 4), new WorldPosition(1, 4), WorldStackDirection.SouthEast, 8, [], null)
            ]);

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Attacker, hitpoints: 10), GridPosition.FrontBottom)
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit2"), unitDefinition.Id, BattleSide.Defender, hitpoints: 10), GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(2, 4)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(1, 4), new WorldPosition(2, 4), WorldStackDirection.SouthEast, 6, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(1, 4), new WorldPosition(2, 4), WorldStackDirection.SouthEast, 6, [], null)
            ]);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            [
                new WorldStackMovementConsumedEvent(stack.Id, 1),
                new WorldFortificationBattleStartedEvent(stack.Id, city.Id, city.Entrance, expectedBattleState)
            ]);
    }

    [Test]
    public void EnterCityFreeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty) with
        {
            VisionRadius = 3
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(5, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(cityDefinition)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 3), new WorldPosition(4, 2), WorldStackDirection.North, 4, [city.Entrance], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 3), new WorldPosition(4, 2), WorldStackDirection.North, 4, [city.Entrance], null),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(4, 2), city.Entrance, WorldStackDirection.West, 4, [], null),
                new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("city")),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(4, 2), city.Entrance, WorldStackDirection.West, 4, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = city.Entrance,
                    Direction = WorldStackDirection.West,
                    MovementPoints = 4
                })
            }));
    }

    [Test]
    public void ExitCapitalFreeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(5, 2)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(4, 4), new WorldPosition(5, 3), WorldStackDirection.East, 6, [new WorldPosition(5, 2)], null),
                new WorldFortificationExitedEvent(new WorldStackId("stack"), new WorldFortificationId("capital")),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(4, 4), new WorldPosition(5, 3), WorldStackDirection.East, 6, [new WorldPosition(5, 2)], null),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 3), new WorldPosition(5, 2), WorldStackDirection.NorthEast, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 3), new WorldPosition(5, 2), WorldStackDirection.NorthEast, 4, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(5, 2),
                    Direction = WorldStackDirection.NorthEast,
                    MovementPoints = 4
                })
            }));
    }

    [Test]
    public void EnterCityWithoutMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty) with
        {
            VisionRadius = 3
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(3, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(cityDefinition)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(3, 4), city.Entrance, WorldStackDirection.NorthEast, 0, [], null),
                new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("city")),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(3, 4), city.Entrance, WorldStackDirection.NorthEast, 0, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = city.Entrance,
                    Direction = WorldStackDirection.NorthEast,
                    MovementPoints = 0
                })
            }));
    }

    [Test]
    public void ExitCityWithoutMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty) with
        {
            VisionRadius = 3
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(cityDefinition)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(4, 4)),
            [
                new WorldStackMovingEvent(stack.Id, city.Entrance, new WorldPosition(4, 4), WorldStackDirection.South, 0, [], null),
                new WorldFortificationExitedEvent(new WorldStackId("stack"), new WorldFortificationId("city")),
                new WorldStackMovedEvent(stack.Id, city.Entrance, new WorldPosition(4, 4), WorldStackDirection.South, 0, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(4, 4),
                    Direction = WorldStackDirection.South,
                    MovementPoints = 0
                })
            }));
    }

    [Test]
    public void AttackCityWithoutMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty) with
        {
            VisionRadius = 3
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 10, initiative: 50, damage: 5);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, soldier.IsLarge)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(3, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(cityDefinition)
            .WithDefinition(leader)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            WorldCommandError.InsufficientMovement);
    }

    [Test]
    public void MoveTowardsBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldBag bag = new(
            Id: new WorldBagId("bag"),
            Position: new WorldPosition(2, 0),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, bag.Position),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], bag.Position),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], bag.Position),
                new WorldBagPickedUpEvent(stack.Id, bag.Id, new WorldInventory([bagItem]))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(1, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 4,
                    Inventory = new WorldInventory([bagItem])
                }),
                Bags = engine.State.Bags.Remove(bag.Id)
            }));
    }

    [Test]
    public void MoveOntoAdjacentBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldBag bag = new(
            Id: new WorldBagId("bag"),
            Position: new WorldPosition(1, 0),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, bag.Position),
            [
                new WorldBagPickedUpEvent(stack.Id, bag.Id, new WorldInventory([bagItem]))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = new WorldInventory([bagItem])
                }),
                Bags = engine.State.Bags.Remove(bag.Id)
            }));
    }

    [Test]
    public void MoveAroundBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldBag bag = new(
            Id: new WorldBagId("bag"),
            Position: new WorldPosition(1, 0),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(2, 0)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 1), WorldStackDirection.South, 4, [new WorldPosition(2, 0)], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 1), WorldStackDirection.South, 4, [new WorldPosition(2, 0)], null),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(1, 1), new WorldPosition(2, 0), WorldStackDirection.East, 2, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(1, 1), new WorldPosition(2, 0), WorldStackDirection.East, 2, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(2, 0),
                    Direction = WorldStackDirection.East,
                    MovementPoints = 2
                })
            }));
    }

    [Test]
    public void BagBesideEnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemyStack"),
            player2.Id,
            new WorldPosition(3, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldBag bag = new(
            Id: new WorldBagId("bag"),
            Position: new WorldPosition(2, 0),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithStack(enemyStack)
            .WithBag(bag)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, bag.Position),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], bag.Position),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], bag.Position),
                new WorldBagPickedUpEvent(stack.Id, bag.Id, new WorldInventory([bagItem]))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(1, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 4,
                    Inventory = new WorldInventory([bagItem])
                }),
                Bags = engine.State.Bags.Remove(bag.Id)
            }));
    }

    [Test]
    public void EnemyStackBesideBagApproachTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemyStack"),
            player2.Id,
            new WorldPosition(2, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldBag bag = new(
            Id: new WorldBagId("bag"),
            Position: new WorldPosition(2, 0),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithStack(enemyStack)
            .WithBag(bag)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: 100), GridPosition.FrontBottom)
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("leader2"), leader.Id, BattleSide.Defender, hitpoints: 100), GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, bag.Position),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], enemyStack.Position),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], enemyStack.Position),
                new WorldStackMovementConsumedEvent(stack.Id, 1),
                new WorldStackBattleStartedEvent(stack.Id, enemyStack.Id, enemyStack.Position, expectedBattleState, IsDuel: false)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(1, 0),
                    Direction = WorldStackDirection.South,
                    MovementPoints = 1
                }),
                Bags = engine.State.Bags.SetItem(bag.Id, bag)
            }));
    }

    [Test]
    public void RodBlocksMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldRod rod = new(
            Id: new WorldRodId("rod"),
            OwnerId: player.Id,
            Position: new WorldPosition(1, 0)
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRod(rod)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, rod.Position),
            WorldCommandError.PathNotFound);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(2, 0)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 1), WorldStackDirection.South, 4, [new WorldPosition(2, 0)], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 1), WorldStackDirection.South, 4, [new WorldPosition(2, 0)], null),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(1, 1), new WorldPosition(2, 0), WorldStackDirection.East, 2, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(1, 1), new WorldPosition(2, 0), WorldStackDirection.East, 2, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(2, 0),
                    Direction = WorldStackDirection.East,
                    MovementPoints = 2
                })
            }));
    }

    [Test]
    public void MoveStateTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null)
            ]);

        WorldPlayer expectedPlayer = player with
        {
            Resources = new WorldResourceSet(100, 25, 0, 0, 0, 0)
        };

        WorldStack expectedStack = stack with
        {
            Position = new WorldPosition(0, 1),
            Direction = WorldStackDirection.SouthWest,
            MovementPoints = 4
        };

        Assert.That(engine.State, Is.EqualTo(new WorldState(
            Map: TestHelpers.SimpleMap(3, 3),
            TurnInfo: new WorldTurnInfo(1, new WorldPlayerId(1)),
            Difficulty: WorldDifficulty.Average,
            Players: new Dictionary<WorldPlayerId, WorldPlayer> { [expectedPlayer.Id] = expectedPlayer }.ToImmutableSortedDictionary(),
            Relations: ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            Stacks: new Dictionary<WorldStackId, WorldStack> { [expectedStack.Id] = expectedStack }.ToImmutableSortedDictionary(),
            Fortifications: ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            ResourceNodes: ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            Rods: ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            Bags: ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            Tombs: ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty,
            Landmarks: ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
            Mountains: ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            Sites: ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            Subraces: ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            ScenarioState: WorldScenarioState.Empty,
            AiMemories: ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty)));
    }

    [Test]
    public void MoveStackAroundFriendlyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldStack mover = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 4);

        WorldStack blocker = TestHelpers.SimpleStack(
            new WorldStackId("Blocker"),
            player.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(mover)
            .WithStack(blocker)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(mover.Id, new WorldPosition(2, 0)),
            [
                new WorldStackMovingEvent(mover.Id, new WorldPosition(0, 0), new WorldPosition(1, 1), WorldStackDirection.South, 2, [new WorldPosition(2, 0)], null),
                new WorldStackMovedEvent(mover.Id, new WorldPosition(0, 0), new WorldPosition(1, 1), WorldStackDirection.South, 2, [new WorldPosition(2, 0)], null),
                new WorldStackMovingEvent(mover.Id, new WorldPosition(1, 1), new WorldPosition(2, 0), WorldStackDirection.East, 0, [], null),
                new WorldStackMovedEvent(mover.Id, new WorldPosition(1, 1), new WorldPosition(2, 0), WorldStackDirection.East, 0, [], null)
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(mover.Id, mover with
                {
                    Position = new WorldPosition(2, 0),
                    Direction = WorldStackDirection.East,
                    MovementPoints = 0
                })
            }));
    }

    [Test]
    public void MoveStackThroughFriendlyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack mover = TestHelpers.SimpleStack(
            new WorldStackId("Mover"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack blocker = TestHelpers.SimpleStack(
            new WorldStackId("Blocker"),
            player.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(mover)
            .WithStack(blocker)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(mover.Id, new WorldPosition(2, 0)),
            WorldCommandError.PathNotFound
        );
    }

    [Test]
    public void MoveStackRevealsFogPerStepTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Unit_s1", maxHitpoints: 10, initiative: 50, damage: 5),
                movementPoints: 10, leadership: 6) with
            {
                VisionRadius = 1
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "??????",
                "......",
                "??????"
            ])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("s1"), new WorldPosition(3, 1)),
            [
                new WorldStackMovingEvent(new WorldStackId("s1"), new WorldPosition(0, 1), new WorldPosition(1, 1), WorldStackDirection.SouthEast, 8, [new WorldPosition(2, 1), new WorldPosition(3, 1)], null),
                new WorldFogRevealedEvent(new WorldPlayerId(1), [new WorldPosition(2, 0), new WorldPosition(2, 2)]),
                new WorldStackMovedEvent(new WorldStackId("s1"), new WorldPosition(0, 1), new WorldPosition(1, 1), WorldStackDirection.SouthEast, 8, [new WorldPosition(2, 1), new WorldPosition(3, 1)], null),
                new WorldStackMovingEvent(new WorldStackId("s1"), new WorldPosition(1, 1), new WorldPosition(2, 1), WorldStackDirection.SouthEast, 6, [new WorldPosition(3, 1)], null),
                new WorldFogRevealedEvent(new WorldPlayerId(1), [new WorldPosition(3, 0), new WorldPosition(3, 2)]),
                new WorldStackMovedEvent(new WorldStackId("s1"), new WorldPosition(1, 1), new WorldPosition(2, 1), WorldStackDirection.SouthEast, 6, [new WorldPosition(3, 1)], null),
                new WorldStackMovingEvent(new WorldStackId("s1"), new WorldPosition(2, 1), new WorldPosition(3, 1), WorldStackDirection.SouthEast, 4, [], null),
                new WorldFogRevealedEvent(new WorldPlayerId(1), [new WorldPosition(4, 0), new WorldPosition(4, 2)]),
                new WorldStackMovedEvent(new WorldStackId("s1"), new WorldPosition(2, 1), new WorldPosition(3, 1), WorldStackDirection.SouthEast, 4, [], null)
            ]
        );

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[new WorldPlayerId(1)].FogOfWar), Is.EqualTo([
            ".....?",
            "......",
            ".....?"
        ]));
    }

    [Test]
    public void GetPathGuardedPassTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            ".....",
            ".....",
            "^^^^^"
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);
        WorldStack guard = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            enemyPlayer.Id,
            new WorldPosition(2, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithStack(stack)
            .WithStack(guard)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, new WorldPosition(4, 1)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
                [new WorldPathSegment(new WorldPosition(0, 1), new WorldPosition(1, 1), 2, Turn: 0)]),
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(1, 1), new WorldPosition(2, 2), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(2, 2), new WorldPosition(3, 1), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(3, 1), new WorldPosition(4, 1), 2, Turn: 0)
            ]),
            new WorldPathEncounter.Battle(new WorldPosition(2, 1)))));
    }

    [Test]
    public void GetPathToEnemyNoMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            enemyPlayer.Id,
            new WorldPosition(5, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithStack(stack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(3, 0)));

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, enemyStack.Position),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
                [new WorldPathSegment(new WorldPosition(3, 0), new WorldPosition(4, 0), 2, Turn: 1)]),
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(4, 0), new WorldPosition(5, 0), 2, Turn: 1)
            ]),
            null)));
    }

    [Test]
    public void GetPathToEnemyInsufficientMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 1, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 1);
        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            enemyPlayer.Id,
            new WorldPosition(4, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithStack(stack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, enemyStack.Position),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath([
                new WorldPathSegment(new WorldPosition(0, 0), new WorldPosition(1, 0), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(1, 0), new WorldPosition(2, 0), 2, Turn: 1),
                new WorldPathSegment(new WorldPosition(2, 0), new WorldPosition(3, 0), 2, Turn: 2)
            ]),
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(3, 0), new WorldPosition(4, 0), 2, Turn: 3)
            ]),
            null)));

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, enemyStack.Position),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 0, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 0, [], null)
            ]);
    }

    [Test]
    public void MoveOtherPlayerStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            otherPlayer.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithStack(stack)
            .WithStack(otherStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(new WorldMoveStackCommand(otherStack.Id, new WorldPosition(2, 1)), WorldCommandError.StackNotFound);

        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(otherStack.Id, new WorldPosition(2, 1)),
            [
                new WorldStackMovingEvent(otherStack.Id, new WorldPosition(2, 2), new WorldPosition(2, 1), WorldStackDirection.NorthEast, 4, [], null),
                new WorldStackMovedEvent(otherStack.Id, new WorldPosition(2, 2), new WorldPosition(2, 1), WorldStackDirection.NorthEast, 4, [], null)
            ]);
    }

    [Test]
    public void GetPathOtherPlayerStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            otherPlayer.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithStack(stack)
            .WithStack(otherStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(new WorldGetPathCommand(otherStack.Id, new WorldPosition(2, 1)), WorldCommandError.StackNotFound);
    }

    [Test]
    public void MoveIntoUnexploredTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                VisionRadius = 1
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["..????"])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(new WorldMoveStackCommand(stack.Id, new WorldPosition(3, 0)), WorldCommandError.PathNotFound);
    }

    [Test]
    public void AiMoveIntoUnexploredTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                VisionRadius = 1
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            FogOfWar = TestHelpers.BuildFogOfWar(["..????"])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(new WorldAiMoveStackCommand(stack.Id, new WorldPosition(3, 0)), WorldCommandError.PathNotFound);
    }

    [Test]
    public void AiOrderedMoveIntoUnexploredTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                VisionRadius = 1
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            FogOfWar = TestHelpers.BuildFogOfWar(["..????"])
        };

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("s1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 10) with
            {
                Order = new WorldStackOrder.MoveToLocation(new ScenarioLocationId("loc"), new WorldPosition(3, 0), 0)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldAiMoveStackCommand(stack.Id, new WorldPosition(3, 0)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 8, [new WorldPosition(2, 0), new WorldPosition(3, 0)], null),
                new WorldFogRevealedEvent(player.Id, [new WorldPosition(2, 0)]),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 8, [new WorldPosition(2, 0), new WorldPosition(3, 0)], null),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 6, [new WorldPosition(3, 0)], null),
                new WorldFogRevealedEvent(player.Id, [new WorldPosition(3, 0)]),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 6, [new WorldPosition(3, 0)], null),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(2, 0), new WorldPosition(3, 0), WorldStackDirection.SouthEast, 4, [], null),
                new WorldFogRevealedEvent(player.Id, [new WorldPosition(4, 0)]),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(2, 0), new WorldPosition(3, 0), WorldStackDirection.SouthEast, 4, [], null)
            ]);
    }

    [Test]
    public void OrderedMoveIntoUnexploredTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                VisionRadius = 1
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            FogOfWar = TestHelpers.BuildFogOfWar(["..????"])
        };

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("s1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 10) with
            {
                Order = new WorldStackOrder.MoveToLocation(new ScenarioLocationId("loc"), new WorldPosition(3, 0), 0)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(new WorldMoveStackCommand(stack.Id, new WorldPosition(3, 0)), WorldCommandError.PathNotFound);
    }

    [Test]
    public void ThiefApproachEnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(thief).AddUnit(leader).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("thief"), thief.Id, BattleSide.Attacker, hitpoints: 100), GridPosition.FrontBottom)
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("leader"), leader.Id, BattleSide.Defender, hitpoints: 100), GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        engine.CheckCommand(
            new WorldMoveStackCommand(thiefStack.Id, enemyStack.Position),
            [
                new WorldStackMovingEvent(thiefStack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 18, [new WorldPosition(2, 0)], new WorldPosition(3, 0)),
                new WorldStackMovedEvent(thiefStack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 18, [new WorldPosition(2, 0)], new WorldPosition(3, 0)),
                new WorldStackMovingEvent(thiefStack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 16, [], new WorldPosition(3, 0)),
                new WorldStackMovedEvent(thiefStack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 16, [], new WorldPosition(3, 0)),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 6),
                new WorldStackBattleStartedEvent(thiefStack.Id, enemyStack.Id, enemyStack.Position, expectedBattleState, IsDuel: false)
            ]);
    }

    [Test]
    public void ThiefPastEnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 3);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(thiefStack.Id, new WorldPosition(4, 1)),
            [
                new WorldStackMovingEvent(thiefStack.Id, new WorldPosition(0, 1), new WorldPosition(1, 1), WorldStackDirection.SouthEast, 18, [new WorldPosition(2, 1), new WorldPosition(3, 1), new WorldPosition(4, 1)], null),
                new WorldStackMovedEvent(thiefStack.Id, new WorldPosition(0, 1), new WorldPosition(1, 1), WorldStackDirection.SouthEast, 18, [new WorldPosition(2, 1), new WorldPosition(3, 1), new WorldPosition(4, 1)], null),
                new WorldStackMovingEvent(thiefStack.Id, new WorldPosition(1, 1), new WorldPosition(2, 1), WorldStackDirection.SouthEast, 16, [new WorldPosition(3, 1), new WorldPosition(4, 1)], null),
                new WorldStackMovedEvent(thiefStack.Id, new WorldPosition(1, 1), new WorldPosition(2, 1), WorldStackDirection.SouthEast, 16, [new WorldPosition(3, 1), new WorldPosition(4, 1)], null),
                new WorldStackMovingEvent(thiefStack.Id, new WorldPosition(2, 1), new WorldPosition(3, 1), WorldStackDirection.SouthEast, 14, [new WorldPosition(4, 1)], null),
                new WorldStackMovedEvent(thiefStack.Id, new WorldPosition(2, 1), new WorldPosition(3, 1), WorldStackDirection.SouthEast, 14, [new WorldPosition(4, 1)], null),
                new WorldStackMovingEvent(thiefStack.Id, new WorldPosition(3, 1), new WorldPosition(4, 1), WorldStackDirection.SouthEast, 12, [], null),
                new WorldStackMovedEvent(thiefStack.Id, new WorldPosition(3, 1), new WorldPosition(4, 1), WorldStackDirection.SouthEast, 12, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with
                {
                    Position = new WorldPosition(4, 1),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 12
                })
            }));
    }

    [Test]
    public void ThiefLeaveEnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(thiefStack.Id, new WorldPosition(3, 1)),
            [
                new WorldStackMovingEvent(thiefStack.Id, new WorldPosition(1, 1), new WorldPosition(2, 1), WorldStackDirection.SouthEast, 18, [new WorldPosition(3, 1)], null),
                new WorldStackMovedEvent(thiefStack.Id, new WorldPosition(1, 1), new WorldPosition(2, 1), WorldStackDirection.SouthEast, 18, [new WorldPosition(3, 1)], null),
                new WorldStackMovingEvent(thiefStack.Id, new WorldPosition(2, 1), new WorldPosition(3, 1), WorldStackDirection.SouthEast, 16, [], null),
                new WorldStackMovedEvent(thiefStack.Id, new WorldPosition(2, 1), new WorldPosition(3, 1), WorldStackDirection.SouthEast, 16, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with
                {
                    Position = new WorldPosition(3, 1),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 16
                })
            }));
    }

    [Test]
    public void ThiefBesideEnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(thief).AddUnit(leader).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("thief"), thief.Id, BattleSide.Attacker, hitpoints: 100), GridPosition.FrontBottom)
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("leader"), leader.Id, BattleSide.Defender, hitpoints: 100), GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        engine.CheckCommand(
            new WorldMoveStackCommand(thiefStack.Id, enemyStack.Position),
            [
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10),
                new WorldStackBattleStartedEvent(thiefStack.Id, enemyStack.Id, enemyStack.Position, expectedBattleState, IsDuel: false)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(thiefStack.Id, thiefStack with { Direction = WorldStackDirection.SouthEast, MovementPoints = 10 })
            }));
    }

    [Test]
    public void GetPathToEnemyAsThiefTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(thiefStack.Id, enemyStack.Position),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(0, 0), new WorldPosition(1, 0), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(1, 0), new WorldPosition(2, 0), 2, Turn: 0)
            ]),
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(2, 0), new WorldPosition(3, 0), 2, Turn: 0)
            ]),
            new WorldPathEncounter.Thief(new WorldPosition(3, 0)))));
    }

    [Test]
    public void GetPathPastEnemyAsThiefTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 3);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack friendlyStack = TestHelpers.SimpleStack(
            new WorldStackId("friendly"),
            player1.Id,
            new WorldPosition(4, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("friendly"), leader),
            movementPoints: 6);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(friendlyStack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(thiefStack.Id, friendlyStack.Position),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(0, 1), new WorldPosition(1, 1), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(1, 1), new WorldPosition(2, 1), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(2, 1), new WorldPosition(3, 1), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(3, 1), new WorldPosition(4, 1), 2, Turn: 0)
            ]),
            new WorldPath([]),
            null)));
    }

    [Test]
    public void GetPathToBagInReachTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(1, 0),
            new WorldObjectVariant(0),
            new WorldInventory([new WorldItem(new WorldItemId("BagItem"), new ItemDefinitionId("Item1"), Charges: null)]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, bag.Position),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath([]),
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(0, 0), new WorldPosition(1, 0), 2, Turn: 0)
            ]),
            new WorldPathEncounter.Bag(bag.Id))));
    }

    [Test]
    public void GetPathToBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(2, 0),
            new WorldObjectVariant(0),
            new WorldInventory([new WorldItem(new WorldItemId("BagItem"), new ItemDefinitionId("Item1"), Charges: null)]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, bag.Position),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
                [new WorldPathSegment(new WorldPosition(0, 0), new WorldPosition(1, 0), 2, Turn: 0)]),
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(1, 0), new WorldPosition(2, 0), 2, Turn: 0)
            ]),
            null)));
    }

    [Test]
    public void GetPathToSiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(6, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 20);
        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Items: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, site.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(6, 4), new WorldPosition(5, 4), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(5, 4), new WorldPosition(4, 4), 2, Turn: 0)
            ]),
            new WorldPath([]),
            new WorldPathEncounter.Site(site.Id))));
    }

    [Test]
    public void GetPathToSiteAsThiefTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player.Id,
            new WorldPosition(6, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);
        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Items: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, site.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(6, 4), new WorldPosition(5, 4), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(5, 4), new WorldPosition(4, 4), 2, Turn: 0)
            ]),
            new WorldPath([]),
            new WorldPathEncounter.Thief(new WorldPosition(4, 4)))));
    }

    [Test]
    public void GetPathToUngarrisonedEnemyCityAsThiefTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Heretic, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, city.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(5, 3), new WorldPosition(4, 2), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(4, 2), new WorldPosition(3, 3), 0, Turn: 0)
            ]),
            new WorldPath([]),
            new WorldPathEncounter.Thief(new WorldPosition(3, 3)))));
    }

    [Test]
    public void GetPathToRiotingEnemyCityAsThiefTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Heretic, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player2.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                RiotTurns = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, city.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(5, 3), new WorldPosition(4, 2), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(4, 2), new WorldPosition(3, 3), 0, Turn: 0)
            ]),
            new WorldPath([]),
            null)));
    }

    [Test]
    public void GetPathToRuinAsThiefTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player.Id,
            new WorldPosition(6, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);
        WorldRuin ruin = new(
            new WorldFortificationId("ruin"),
            new ScenarioTextId("Ruin"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            UnitGrid<WorldUnit>.Empty,
            new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, ruin.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(6, 4), new WorldPosition(5, 4), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(5, 4), new WorldPosition(4, 4), 2, Turn: 0)
            ]),
            new WorldPath([]),
            new WorldPathEncounter.Thief(new WorldPosition(4, 4)))));
    }

    [Test]
    public void GetPathToAlliedCityAsThiefTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Heretic, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldRelation alliance = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithCity(city)
            .WithRelation(alliance)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetPathCommand.Result result = engine.CheckCommand(
            new WorldGetPathCommand(stack.Id, city.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetPathCommand.Result(
            new WorldPath(
            [
                new WorldPathSegment(new WorldPosition(5, 3), new WorldPosition(4, 2), 2, Turn: 0),
                new WorldPathSegment(new WorldPosition(4, 2), new WorldPosition(3, 3), 0, Turn: 0)
            ]),
            new WorldPath([]),
            Encounter: null)));
    }

    [Test]
    public void BattleCostRoundsUpTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 7, leadership: 6);
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("attacker"), attacker),
            movementPoints: 7);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("defender"), defender),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState before = engine.State;

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(attacker).AddUnit(defender).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("attacker"), attacker.Id, BattleSide.Attacker, hitpoints: 100), GridPosition.FrontBottom)
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("defender"), defender.Id, BattleSide.Defender, hitpoints: 100), GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, enemyStack.Position),
            [
                new WorldStackMovementConsumedEvent(stack.Id, 3),
                new WorldStackBattleStartedEvent(stack.Id, enemyStack.Id, enemyStack.Position, expectedBattleState, IsDuel: false)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with { Direction = WorldStackDirection.SouthEast, MovementPoints = 3 })
            }));
    }
}
