namespace Gallean.Engine.Tests;

public sealed class WorldEngineRepositionUnitStackTests
{
    [Test]
    public void SuccessTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), GridPosition.BackCenter),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), GridPosition.BackCenter)
            ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.BackCenter, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }

    [Test]
    public void RepositionLeaderTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"), GridPosition.BackCenter),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"), GridPosition.BackCenter)
            ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackCenter, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }

    [Test]
    public void SwapUnitsTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"), GridPosition.FrontCenter),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"), GridPosition.FrontCenter)
            ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontCenter, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }

    [Test]
    public void SwapLargeUnitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldUnit largeUnit = new(new WorldUnitId("largeUnit"), largeDefinition.Id, 200, 0, 1, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(largeUnit, GridPosition.FrontTop, true)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("largeUnit"), GridPosition.FrontBottom),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("largeUnit"), GridPosition.FrontBottom)
            ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(largeUnit, GridPosition.FrontBottom, true)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontTop, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
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

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldUnit largeUnit = new(new WorldUnitId("largeUnit"), largeDefinition.Id, 200, 0, 1, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(largeUnit, GridPosition.FrontTop, true)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("largeUnit"), GridPosition.FrontCenter),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("largeUnit"), GridPosition.FrontCenter)
            ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Place(largeUnit, GridPosition.FrontCenter, true)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }

    [Test]
    public void ValidationTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^...."
        ]);

        UnitDefinition smallUnitDefinition = TestHelpers.SimpleUnitDefinition("small", maxHitpoints: 100, initiative: 50, damage: 10);

        UnitDefinition largeUnitDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldPlayer otherPlayer = player with
        {
            Id = new WorldPlayerId(2)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("smallUnit"), smallUnitDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("largeUnit"), largeUnitDefinition), GridPosition.FrontTop, true)
                .Build(),
            movementPoints: 20);

        WorldStack enemyStack = stack with
        {
            Id = new WorldStackId("stack2"),
            OwnerId = otherPlayer.Id,
            Position = new WorldPosition(4, 3),
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build()
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(smallUnitDefinition)
            .WithDefinition(largeUnitDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithStack(stack)
            .WithStack(enemyStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldRepositionUnitCommand(
                new WorldUnitGroup.Stack(new WorldStackId("missing")),
                new WorldUnitId("leader2"),
                GridPosition.BackBottom),
            WorldCommandError.StackNotFound);

        engine.CheckCommandError(
            new WorldRepositionUnitCommand(
                new WorldUnitGroup.Stack(enemyStack.Id),
                new WorldUnitId("leader2"),
                GridPosition.BackBottom),
            WorldCommandError.StackNotFound);

        engine.CheckCommandError(
            new WorldRepositionUnitCommand(
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("missing"),
                GridPosition.BackCenter),
            WorldCommandError.UnitNotFound);

        engine.CheckCommandError(
            new WorldRepositionUnitCommand(
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("leader"),
                new GridPosition(0, 3)),
            WorldCommandError.InvalidUnitPosition);
    }

    [Test]
    public void SwapLargeUnitWithBackUnitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6
        };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.BackCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontTop, true)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("large"), GridPosition.BackCenter),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("large"), GridPosition.BackCenter)
            ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontCenter, true)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.BackTop, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
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

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6
        };

        UnitDefinition frontSoldierDefinition = TestHelpers.SimpleUnitDefinition("front-soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        UnitDefinition backSoldierDefinition = TestHelpers.SimpleUnitDefinition("back-soldier", maxHitpoints: 40, initiative: 35, damage: 4);

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("frontSoldier"), frontSoldierDefinition), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("backSoldier"), backSoldierDefinition), GridPosition.BackCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontTop, true)
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
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("large"), GridPosition.FrontCenter),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("large"), GridPosition.FrontCenter)
            ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontCenter, true)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("frontSoldier"), frontSoldierDefinition), GridPosition.FrontTop, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("backSoldier"), backSoldierDefinition), GridPosition.BackTop, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }

    [Test]
    public void SwapUnitWithLargeUnitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6
        };

        UnitDefinition frontSoldierDefinition = TestHelpers.SimpleUnitDefinition("front-soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        UnitDefinition backSoldierDefinition = TestHelpers.SimpleUnitDefinition("back-soldier", maxHitpoints: 40, initiative: 35, damage: 4);

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("frontSoldier"), frontSoldierDefinition), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("backSoldier"), backSoldierDefinition), GridPosition.BackCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontTop, true)
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
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("frontSoldier"), GridPosition.FrontTop),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("frontSoldier"), GridPosition.FrontTop)
            ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontCenter, true)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("frontSoldier"), frontSoldierDefinition), GridPosition.FrontTop, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("backSoldier"), backSoldierDefinition), GridPosition.BackTop, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }
}
