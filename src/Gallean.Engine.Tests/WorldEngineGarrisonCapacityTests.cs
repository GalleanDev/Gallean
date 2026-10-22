namespace Gallean.Engine.Tests;

public sealed class WorldEngineGarrisonCapacityTests
{
    [Test]
    public void VillageHoldsOneUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition smallDefinition = TestHelpers.SimpleUnitDefinition("small", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small"), smallDefinition), GridPosition.FrontBottom, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small2"), smallDefinition), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(smallDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(level1)
            .WithSubrace(TestHelpers.NeutralSubrace)
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
                new WorldUnitId("small2"),
                GridPosition.FrontCenter),
            WorldCommandError.GarrisonCapacityExceeded);
    }

    [Test]
    public void TownHoldsTwoUnitsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition smallDefinition = TestHelpers.SimpleUnitDefinition("small", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level2,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small"), smallDefinition), GridPosition.FrontBottom, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small2"), smallDefinition), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(smallDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(level2)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitId("small2"),
                GridPosition.FrontCenter),
            [
                new WorldUnitTransferredEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitGroup.Fortification(city.Id),
                    new WorldUnitId("small2"),
                    GridPosition.FrontCenter)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                        .Build()
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("small"), smallDefinition), GridPosition.FrontBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("small2"), smallDefinition), GridPosition.FrontCenter, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void LargeUnitInVillageTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 50, damage: 20) with
        {
            IsLarge = true
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontCenter, true)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(largeDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(level1)
            .WithSubrace(TestHelpers.NeutralSubrace)
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
                new WorldUnitId("large"),
                GridPosition.FrontBottom),
            WorldCommandError.GarrisonCapacityExceeded);
    }

    [Test]
    public void LargeUnitInTownTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 50, damage: 20) with
        {
            IsLarge = true
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level2,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontCenter, true)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(largeDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(level2)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitId("large"),
                GridPosition.FrontBottom),
            [
                new WorldUnitTransferredEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitGroup.Fortification(city.Id),
                    new WorldUnitId("large"),
                    GridPosition.FrontBottom)
            ]);
    }

    [Test]
    public void CityUpgradeMakesRoomTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition smallDefinition = TestHelpers.SimpleUnitDefinition("small", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small"), smallDefinition), GridPosition.FrontBottom, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small2"), smallDefinition), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(smallDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithSubrace(TestHelpers.NeutralSubrace)
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
                new WorldUnitId("small2"),
                GridPosition.FrontCenter),
            WorldCommandError.GarrisonCapacityExceeded);

        engine.CheckCommand(new WorldUpgradeCityCommand(city.Id), [
            new WorldCityUpgradedEvent(city.Id, level2.Id, new WorldResourceSet(150, 0, 0, 0, 0, 0))
        ]);

        engine.CheckCommand(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitId("small2"),
                GridPosition.FrontCenter),
            [
                new WorldUnitTransferredEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitGroup.Fortification(city.Id),
                    new WorldUnitId("small2"),
                    GridPosition.FrontCenter)
            ]);
    }

    [Test]
    public void CapitalHoldsSixUnitsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition smallDefinition = TestHelpers.SimpleUnitDefinition("small", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small"), smallDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small2"), smallDefinition), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small3"), smallDefinition), GridPosition.FrontTop, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small4"), smallDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small5"), smallDefinition), GridPosition.BackCenter, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small6"), smallDefinition), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(smallDefinition)
            .WithDefinition(leaderDefinition)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitGroup.Fortification(capital.Id),
                new WorldUnitId("small6"),
                GridPosition.BackTop),
            [
                new WorldUnitTransferredEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitGroup.Fortification(capital.Id),
                    new WorldUnitId("small6"),
                    GridPosition.BackTop)
            ]);
    }

    [Test]
    public void OverfilledVillageReleasesUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition smallDefinition = TestHelpers.SimpleUnitDefinition("small", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small"), smallDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small2"), smallDefinition), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small3"), smallDefinition), GridPosition.FrontTop, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(smallDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(level1)
            .WithSubrace(TestHelpers.NeutralSubrace)
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
                new WorldUnitId("small"),
                GridPosition.FrontCenter),
            [
                new WorldUnitTransferredEvent(
                    new WorldUnitGroup.Fortification(city.Id),
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("small"),
                    GridPosition.FrontCenter)
            ]);

        engine.CheckCommand(
            new WorldRepositionUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitId("small2"),
                GridPosition.BackBottom),
            [
                new WorldUnitRepositionedEvent(
                    new WorldUnitGroup.Fortification(city.Id),
                    new WorldUnitId("small2"),
                    GridPosition.BackBottom)
            ]);
    }

    [Test]
    public void OverfilledVillageRefusesUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition smallDefinition = TestHelpers.SimpleUnitDefinition("small", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small"), smallDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small2"), smallDefinition), GridPosition.FrontCenter, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small3"), smallDefinition), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(smallDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(level1)
            .WithSubrace(TestHelpers.NeutralSubrace)
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
                new WorldUnitId("small3"),
                GridPosition.FrontTop),
            WorldCommandError.GarrisonCapacityExceeded);
    }

    [Test]
    public void SwapLargeUnitIntoVillageTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition smallDefinition = TestHelpers.SimpleUnitDefinition("small", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 50, damage: 20) with
        {
            IsLarge = true
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("small"), smallDefinition), GridPosition.FrontBottom, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontCenter, true)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(smallDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(level1)
            .WithSubrace(TestHelpers.NeutralSubrace)
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
                new WorldUnitId("large"),
                GridPosition.FrontBottom),
            WorldCommandError.GarrisonCapacityExceeded);
    }
}
