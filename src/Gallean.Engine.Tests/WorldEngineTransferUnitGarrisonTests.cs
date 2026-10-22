namespace Gallean.Engine.Tests;

public sealed class WorldEngineTransferUnitGarrisonTests
{
    [Test]
    public void CityToStackTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Experience = 17 }, new GridPosition(0, 0), false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldTransferUnitCommand command = new(
            new WorldUnitGroup.Fortification(city.Id),
            new WorldUnitGroup.Stack(stack.Id),
            new WorldUnitId("unit"),
            new GridPosition(0, 1)
        );

        engine.CheckCommand(command, [
            new WorldUnitTransferredEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("unit"), new GridPosition(0, 1))
        ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Experience = 17 }, new GridPosition(0, 1), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = UnitGrid<WorldUnit>.Empty
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }

    [Test]
    public void StackToCityTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldTransferUnitCommand command = new(
            new WorldUnitGroup.Stack(stack.Id),
            new WorldUnitGroup.Fortification(city.Id),
            new WorldUnitId("unit"),
            new GridPosition(0, 1)
        );

        engine.CheckCommand(command, [
            new WorldUnitTransferredEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("unit"), new GridPosition(0, 1))
        ]);

        UnitGrid<WorldUnit> expectedCityGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition), new GridPosition(0, 1), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = expectedCityGarrison
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 1), false)
                        .Build()
                })
            }));
    }

    [Test]
    public void CapitalToStackTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent),
            PurchaseCost = WorldResourceSet.Empty
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Hitpoints = 75 }, GridPosition.BackTop, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            capital.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldTransferUnitCommand command = new(
            new WorldUnitGroup.Fortification(capital.Id),
            new WorldUnitGroup.Stack(stack.Id),
            new WorldUnitId("unit"),
            GridPosition.FrontCenter
        );

        engine.CheckCommand(command, [
            new WorldUnitTransferredEvent(
                new WorldUnitGroup.Fortification(capital.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("unit"),
                GridPosition.FrontCenter)
        ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition), GridPosition.FrontCenter, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
                {
                    Garrison = UnitGrid<WorldUnit>.Empty
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }

    [Test]
    public void CapitalGuardianTransferForbiddenTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 2
        };

        UnitDefinition guardianDefinition = TestHelpers.SimpleUnitDefinition("guardian", maxHitpoints: 200, initiative: 40, damage: 20) with
        {
            Category = UnitCategory.Guardian
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guardian"), guardianDefinition), GridPosition.FrontBottom, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            capital.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(guardianDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(capital.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("guardian"),
                GridPosition.FrontTop),
            WorldCommandError.GuardianTransferForbidden);
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

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("large"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent),
            IsLarge = true,
            PurchaseCost = WorldResourceSet.Empty
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldUnit unit = new(new WorldUnitId("unit"), unitDefinition.Id, 150, 0, 1, [], null, false);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(unit, GridPosition.FrontTop, true)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldTransferUnitCommand command = new(
            new WorldUnitGroup.Fortification(city.Id),
            new WorldUnitGroup.Stack(stack.Id),
            new WorldUnitId("unit"),
            GridPosition.FrontBottom
        );

        engine.CheckCommand(command, [
            new WorldUnitTransferredEvent(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("unit"),
                GridPosition.FrontBottom)
        ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(unit with { Hitpoints = 180 }, GridPosition.FrontBottom, true)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontCenter, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = UnitGrid<WorldUnit>.Empty
                }),
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

        UnitDefinition smallUnitDefinition = TestHelpers.SimpleUnitDefinition("small", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("small"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent),
            PurchaseCost = WorldResourceSet.Empty
        };

        UnitDefinition largeUnitDefinition = smallUnitDefinition with
        {
            Id = new UnitDefinitionId("large"),
            IsLarge = true
        };

        UnitDefinition stackLeaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("stack-leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldPlayer otherPlayer = player with
        {
            Id = new WorldPlayerId(2)
        };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("smallUnit"), smallUnitDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("largeUnit"), largeUnitDefinition) with { Hitpoints = 200 }, GridPosition.FrontTop, true)
                .Build());

        WorldCity enemyCity = city with
        {
            Id = new WorldFortificationId("city2"),
            OwnerId = otherPlayer.Id,
            Footprint = FortificationCalculator.CreateFootprint(new WorldPosition(4, 3), FortificationCalculator.CityFootprint),
            Garrison = UnitGrid<WorldUnit>.Empty
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("smallUnit2"), smallUnitDefinition), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("stackLeader"), stackLeaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack enemyStack = stack with
        {
            Id = new WorldStackId("stack2"),
            OwnerId = otherPlayer.Id,
            Position = new WorldPosition(6, 3),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("stackLeader2"), stackLeaderDefinition)
        };

        WorldStack stackInEnemyCity = stack with
        {
            Id = new WorldStackId("stack3"),
            Position = enemyCity.Entrance,
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("stackLeader3"), stackLeaderDefinition)
        };

        WorldStack stackWithoutGarrison = stack with
        {
            Id = new WorldStackId("stack4"),
            Position = new WorldPosition(5, 3),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("stackLeader4"), stackLeaderDefinition)
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(smallUnitDefinition)
            .WithDefinition(largeUnitDefinition)
            .WithDefinition(stackLeaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithCity(city)
            .WithCity(enemyCity)
            .WithStack(stack)
            .WithStack(enemyStack)
            .WithStack(stackInEnemyCity)
            .WithStack(stackWithoutGarrison)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(new WorldStackId("missing")),
                new WorldUnitId("smallUnit"),
                GridPosition.BackBottom),
            WorldCommandError.StackNotFound
        );

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(enemyStack.Id),
                new WorldUnitId("smallUnit"),
                GridPosition.BackBottom),
            WorldCommandError.StackNotFound
        );

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(enemyCity.Id),
                new WorldUnitGroup.Stack(stackInEnemyCity.Id),
                new WorldUnitId("smallUnit"),
                GridPosition.BackBottom),
            WorldCommandError.GarrisonNotFound
        );

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stackWithoutGarrison.Id),
                new WorldUnitId("smallUnit"),
                GridPosition.BackBottom),
            WorldCommandError.GarrisonNotFound
        );

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("missing"),
                GridPosition.BackBottom),
            WorldCommandError.UnitNotFound
        );

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("smallUnit"),
                new GridPosition(0, 3)),
            WorldCommandError.InvalidUnitPosition
        );
    }

    [Test]
    public void LeadershipCapacityTest()
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

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldierDefinition), new GridPosition(0, 1), false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier3"), soldierDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("soldier"),
                new GridPosition(0, 2)),
            [
                new WorldUnitTransferredEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), new GridPosition(0, 2))
            ]);

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("soldier2"),
                new GridPosition(1, 0)),
            WorldCommandError.LeadershipExceeded);

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitId("leader"),
                new GridPosition(1, 0)),
            WorldCommandError.LeaderTransferForbidden);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier3"), soldierDefinition), new GridPosition(0, 1), false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 2), false)
            .Build();

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldierDefinition), new GridPosition(0, 1), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = expectedGarrison
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
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

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 2
        };

        UnitDefinition garrisonLeaderDefinition = TestHelpers.SimpleUnitDefinition("garrison-leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("garrisonLeader"), garrisonLeaderDefinition), new GridPosition(0, 0), false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(garrisonLeaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("garrisonLeader"),
                new GridPosition(0, 1)),
            WorldCommandError.LeaderTransferForbidden);
    }

    [Test]
    public void GarrisonLeaderTransferForbiddenTest()
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
            Leadership = 2
        };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        UnitDefinition specialDefinition = TestHelpers.SimpleUnitDefinition("special", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            Leadership = 6
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("special"), specialDefinition), new GridPosition(0, 0), false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(specialDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("special"),
                new GridPosition(0, 2)),
            WorldCommandError.LeaderTransferForbidden);
    }

    [Test]
    public void LargeUnitLeadershipTest()
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

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), new GridPosition(0, 0), true)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 1), false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldierDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("large"),
                new GridPosition(0, 2)),
            WorldCommandError.LeadershipExceeded);

        engine.CheckCommand(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("soldier"),
                new GridPosition(0, 2)),
            [
                new WorldUnitTransferredEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), new GridPosition(0, 2))
            ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldierDefinition), new GridPosition(0, 1), false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 2), false)
            .Build();

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), new GridPosition(0, 0), true)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = expectedGarrison
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }

    [Test]
    public void StackToGarrisonSwapTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition stackUnitDefinition = TestHelpers.SimpleUnitDefinition("stack-unit", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition garrisonUnitDefinition = TestHelpers.SimpleUnitDefinition("garrison-unit", maxHitpoints: 80, initiative: 40, damage: 8);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldUnit stackUnit = new(new WorldUnitId("stackUnit"), stackUnitDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit garrisonUnit = new(new WorldUnitId("garrisonUnit"), garrisonUnitDefinition.Id, 80, 0, 1, [], null, false);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonUnit, new GridPosition(0, 0), false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(stackUnit, new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(stackUnitDefinition)
            .WithDefinition(garrisonUnitDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldTransferUnitCommand command = new(
            new WorldUnitGroup.Stack(stack.Id),
            new WorldUnitGroup.Fortification(city.Id),
            new WorldUnitId("stackUnit"),
            new GridPosition(0, 0)
        );

        engine.CheckCommand(command, [
            new WorldUnitTransferredEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("stackUnit"), new GridPosition(0, 0))
        ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
            .Place(garrisonUnit, new GridPosition(0, 1), false)
            .Build();

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(stackUnit, new GridPosition(0, 0), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = expectedGarrison
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }

    [Test]
    public void GarrisonToStackSwapTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition stackUnitDefinition = TestHelpers.SimpleUnitDefinition("stack-unit", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition garrisonUnitDefinition = TestHelpers.SimpleUnitDefinition("garrison-unit", maxHitpoints: 80, initiative: 40, damage: 8);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldUnit stackUnit = new(new WorldUnitId("stackUnit"), stackUnitDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit garrisonUnit = new(new WorldUnitId("garrisonUnit"), garrisonUnitDefinition.Id, 80, 0, 1, [], null, false);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonUnit, new GridPosition(0, 0), false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(stackUnit, new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(stackUnitDefinition)
            .WithDefinition(garrisonUnitDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldTransferUnitCommand command = new(
            new WorldUnitGroup.Fortification(city.Id),
            new WorldUnitGroup.Stack(stack.Id),
            new WorldUnitId("garrisonUnit"),
            new GridPosition(0, 1)
        );

        engine.CheckCommand(command, [
            new WorldUnitTransferredEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("garrisonUnit"), new GridPosition(0, 1))
        ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
            .Place(garrisonUnit, new GridPosition(0, 1), false)
            .Build();

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(stackUnit, new GridPosition(0, 0), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = expectedGarrison
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }

    [Test]
    public void SwapWithLeaderTargetForbiddenTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition stackUnitDefinition = TestHelpers.SimpleUnitDefinition("stack-unit", maxHitpoints: 100, initiative: 50, damage: 10);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };

        UnitDefinition garrisonLeaderDefinition = TestHelpers.SimpleUnitDefinition("garrison-leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("garrisonLeader"), garrisonLeaderDefinition), new GridPosition(0, 0), false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("stackUnit"), stackUnitDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(stackUnitDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(garrisonLeaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitId("stackUnit"),
                new GridPosition(0, 0)),
            WorldCommandError.LeaderTransferForbidden);
    }

    [Test]
    public void SwapLeadershipExceededTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), new GridPosition(0, 0), true)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldierDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("large"),
                new GridPosition(0, 1)),
            WorldCommandError.LeadershipExceeded);
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

        UnitDefinition frontSoldierDefinition = TestHelpers.SimpleUnitDefinition("front-soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        UnitDefinition backSoldierDefinition = TestHelpers.SimpleUnitDefinition("back-soldier", maxHitpoints: 40, initiative: 35, damage: 4);

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), new GridPosition(0, 2), true)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("frontSoldier"), frontSoldierDefinition), new GridPosition(0, 1), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("backSoldier"), backSoldierDefinition), new GridPosition(1, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(frontSoldierDefinition)
            .WithDefinition(backSoldierDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("large"),
                new GridPosition(0, 1)),
            [
                new WorldUnitTransferredEvent(
                    new WorldUnitGroup.Fortification(city.Id),
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("large"),
                    new GridPosition(0, 1))
            ]);

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("frontSoldier"), frontSoldierDefinition), new GridPosition(0, 2), false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("backSoldier"), backSoldierDefinition), new GridPosition(1, 2), false)
            .Build();

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), new GridPosition(0, 1), true)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = expectedGarrison
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }

    [Test]
    public void LargeUnitToBackPositionTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldUnit largeUnit = new(new WorldUnitId("largeUnit"), largeDefinition.Id, 200, 0, 1, [], null, false);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(largeUnit, new GridPosition(0, 2), true)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(largeDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldTransferUnitCommand command = new(
            new WorldUnitGroup.Fortification(city.Id),
            new WorldUnitGroup.Stack(stack.Id),
            new WorldUnitId("largeUnit"),
            new GridPosition(1, 0)
        );

        engine.CheckCommand(command, [
            new WorldUnitTransferredEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("largeUnit"), new GridPosition(1, 0))
        ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 1), false)
            .Place(largeUnit, new GridPosition(0, 0), true)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = UnitGrid<WorldUnit>.Empty
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }

    [Test]
    public void LargeToLargeSwapTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition stackLargeDefinition = TestHelpers.SimpleUnitDefinition("stack-large", maxHitpoints: 200, initiative: 30, damage: 20) with
        {
            IsLarge = true
        };

        UnitDefinition garrisonLargeDefinition = TestHelpers.SimpleUnitDefinition("garrison-large", maxHitpoints: 180, initiative: 25, damage: 18) with
        {
            IsLarge = true
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldUnit stackLargeUnit = new(new WorldUnitId("stackLargeUnit"), stackLargeDefinition.Id, 200, 0, 1, [], null, false);
        WorldUnit garrisonLargeUnit = new(new WorldUnitId("garrisonLargeUnit"), garrisonLargeDefinition.Id, 180, 0, 1, [], null, false);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(garrisonLargeUnit, new GridPosition(0, 0), true)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Place(stackLargeUnit, new GridPosition(0, 1), true)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(stackLargeDefinition)
            .WithDefinition(garrisonLargeDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldTransferUnitCommand command = new(
            new WorldUnitGroup.Stack(stack.Id),
            new WorldUnitGroup.Fortification(city.Id),
            new WorldUnitId("stackLargeUnit"),
            new GridPosition(1, 0)
        );

        engine.CheckCommand(command, [
            new WorldUnitTransferredEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("stackLargeUnit"), new GridPosition(1, 0))
        ]);

        UnitGrid<WorldUnit> expectedStackUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
            .Place(garrisonLargeUnit, new GridPosition(0, 1), true)
            .Build();

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(stackLargeUnit, new GridPosition(0, 0), true)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = expectedGarrison
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 0,
                    Units = expectedStackUnits
                })
            }));
    }
}
