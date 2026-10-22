namespace Gallean.Engine.Tests;

public sealed class WorldEngineTransferUnitStackTests
{
    [Test]
    public void TransferTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack sourceStack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("stack2"),
            player.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), new GridPosition(0, 0), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(sourceStack)
            .WithStack(targetStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("soldier"), new GridPosition(0, 2)),
            [
                new WorldUnitTransferredEvent(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("soldier"), new GridPosition(0, 2))
            ]);

        UnitGrid<WorldUnit> expectedSourceUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
            .Build();

        UnitGrid<WorldUnit> expectedTargetUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), new GridPosition(0, 0), false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 2), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(sourceStack.Id, sourceStack with
                    {
                        MovementPoints = 10,
                        Units = expectedSourceUnits
                    })
                    .SetItem(targetStack.Id, targetStack with
                    {
                        MovementPoints = 10,
                        Units = expectedTargetUnits
                    })
            }));
    }

    [Test]
    public void SwapTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        UnitDefinition archerDefinition = TestHelpers.SimpleUnitDefinition("archer", maxHitpoints: 40, initiative: 60, damage: 15);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldUnit archer = new(new WorldUnitId("archer"), archerDefinition.Id, 40, 0, 1, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 50, 0, 1, [], null, false);

        WorldStack sourceStack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(archer, new GridPosition(1, 1), false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("stack2"),
            player.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(soldier, new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(archerDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(sourceStack)
            .WithStack(targetStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("archer"), new GridPosition(0, 1)),
            [
                new WorldUnitTransferredEvent(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("archer"), new GridPosition(0, 1))
            ]);

        UnitGrid<WorldUnit> expectedSourceUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
            .Place(soldier, new GridPosition(1, 1), false)
            .Build();

        UnitGrid<WorldUnit> expectedTargetUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), new GridPosition(0, 0), false)
            .Place(archer, new GridPosition(0, 1), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(sourceStack.Id, sourceStack with
                    {
                        MovementPoints = 10,
                        Units = expectedSourceUnits
                    })
                    .SetItem(targetStack.Id, targetStack with
                    {
                        MovementPoints = 10,
                        Units = expectedTargetUnits
                    })
            }));
    }

    [Test]
    public void LargeUnitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldUnit largeUnit = new(new WorldUnitId("largeUnit"), largeDefinition.Id, 200, 0, 1, [], null, false);

        WorldStack sourceStack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontCenter, false)
                .Place(largeUnit, GridPosition.FrontTop, true)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("stack2"),
            player.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(sourceStack)
            .WithStack(targetStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("largeUnit"), GridPosition.FrontBottom),
            [
                new WorldUnitTransferredEvent(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("largeUnit"), GridPosition.FrontBottom)
            ]);

        UnitGrid<WorldUnit> expectedSourceUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontCenter, false)
            .Build();

        UnitGrid<WorldUnit> expectedTargetUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontCenter, false)
            .Place(largeUnit, GridPosition.FrontBottom, true)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(sourceStack.Id, sourceStack with
                    {
                        MovementPoints = 10,
                        Units = expectedSourceUnits
                    })
                    .SetItem(targetStack.Id, targetStack with
                    {
                        MovementPoints = 10,
                        Units = expectedTargetUnits
                    })
            }));
    }

    [Test]
    public void LeaderTransferForbiddenTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack sourceStack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("stack2"),
            player.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), new GridPosition(0, 0), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(sourceStack)
            .WithStack(targetStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("leader"), new GridPosition(0, 1)),
            WorldCommandError.LeaderTransferForbidden);

        engine.CheckCommandError(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("soldier"), new GridPosition(0, 0)),
            WorldCommandError.LeaderTransferForbidden);
    }

    [Test]
    public void LeadershipExceededTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 2);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack sourceStack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("stack2"),
            player.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldierDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(sourceStack)
            .WithStack(targetStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("soldier"), new GridPosition(0, 2)),
            WorldCommandError.LeadershipExceeded);
    }

    [Test]
    public void NotAdjacentTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack sourceStack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(1, 1),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("stack2"),
            player.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), new GridPosition(0, 0), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(sourceStack)
            .WithStack(targetStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("soldier"), new GridPosition(0, 1)),
            WorldCommandError.StackNotAdjacent);

        engine.CheckCommandError(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitId("soldier"), new GridPosition(1, 1)),
            WorldCommandError.StackNotAdjacent);
    }

    [Test]
    public void ValidationTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldPlayer otherPlayer = player with
        {
            Id = new WorldPlayerId(2)
        };

        WorldStack sourceStack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("stack2"),
            player.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), new GridPosition(0, 0), false)
                .Build(),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("stack3"),
            otherPlayer.Id,
            new WorldPosition(2, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader3"), leaderDefinition), new GridPosition(0, 0), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithStack(sourceStack)
            .WithStack(targetStack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(new WorldStackId("missing")), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("soldier"), new GridPosition(0, 1)),
            WorldCommandError.StackNotFound);

        engine.CheckCommandError(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(new WorldStackId("missing")), new WorldUnitId("soldier"), new GridPosition(0, 1)),
            WorldCommandError.StackNotFound);

        engine.CheckCommandError(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(enemyStack.Id), new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitId("leader3"), new GridPosition(0, 2)),
            WorldCommandError.StackNotFound);

        engine.CheckCommandError(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(enemyStack.Id), new WorldUnitId("soldier"), new GridPosition(0, 1)),
            WorldCommandError.StackNotFound);

        engine.CheckCommandError(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("soldier"), new GridPosition(0, 3)),
            WorldCommandError.InvalidUnitPosition);

        engine.CheckCommandError(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("missing"), new GridPosition(0, 1)),
            WorldCommandError.UnitNotFound);
    }

    [Test]
    public void SwapLargeUnitWithTwoUnitsTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 5);

        UnitDefinition frontSoldierDefinition = TestHelpers.SimpleUnitDefinition("front-soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        UnitDefinition backSoldierDefinition = TestHelpers.SimpleUnitDefinition("back-soldier", maxHitpoints: 40, initiative: 35, damage: 4);

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack sourceStack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontTop, true)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("stack2"),
            player.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("frontSoldier"), frontSoldierDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("backSoldier"), backSoldierDefinition), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(frontSoldierDefinition)
            .WithDefinition(backSoldierDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(sourceStack)
            .WithStack(targetStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("large"), GridPosition.FrontBottom),
            [
                new WorldUnitTransferredEvent(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitId("large"), GridPosition.FrontBottom)
            ]);

        UnitGrid<WorldUnit> expectedSourceUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontCenter, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("frontSoldier"), frontSoldierDefinition), GridPosition.FrontTop, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("backSoldier"), backSoldierDefinition), GridPosition.BackTop, false)
            .Build();

        UnitGrid<WorldUnit> expectedTargetUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontCenter, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontBottom, true)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(sourceStack.Id, sourceStack with
                    {
                        MovementPoints = 10,
                        Units = expectedSourceUnits
                    })
                    .SetItem(targetStack.Id, targetStack with
                    {
                        MovementPoints = 10,
                        Units = expectedTargetUnits
                    })
            }));
    }
}
