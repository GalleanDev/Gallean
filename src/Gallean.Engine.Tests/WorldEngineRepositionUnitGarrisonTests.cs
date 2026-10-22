namespace Gallean.Engine.Tests;

public sealed class WorldEngineRepositionUnitGarrisonTests
{
    [Test]
    public void WithinCityTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

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

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontCenter, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldierDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("soldier"), GridPosition.BackCenter),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("soldier"), GridPosition.BackCenter)
            ]);

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.BackCenter, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = expectedGarrison
                })
            }));
    }

    [Test]
    public void WithinCapitalTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 3
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.BackTop, false)
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
            .WithDefinition(soldierDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Fortification(capital.Id), new WorldUnitId("soldier"), GridPosition.FrontTop),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Fortification(capital.Id), new WorldUnitId("soldier"), GridPosition.FrontTop)
            ]);

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontTop, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
                {
                    Garrison = expectedGarrison
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

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);
        UnitDefinition guardDefinition = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 60, initiative: 45, damage: 6);

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

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guardDefinition), GridPosition.BackCenter, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldierDefinition)
            .WithDefinition(guardDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("soldier"), GridPosition.BackCenter),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("soldier"), GridPosition.BackCenter)
            ]);

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guardDefinition), GridPosition.FrontCenter, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.BackCenter, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = expectedGarrison
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

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(largeUnit, GridPosition.FrontTop, true)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            player.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("largeUnit"), GridPosition.FrontCenter),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("largeUnit"), GridPosition.FrontCenter)
            ]);

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(largeUnit, GridPosition.FrontCenter, true)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = expectedGarrison
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

        UnitDefinition largeUnitDefinition = smallUnitDefinition with
        {
            Id = new UnitDefinitionId("large"),
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
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("smallUnit2"), smallUnitDefinition), GridPosition.BackBottom, false)
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
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(smallUnitDefinition)
            .WithDefinition(largeUnitDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithCity(city)
            .WithCity(enemyCity)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldRepositionUnitCommand(
                new WorldUnitGroup.Fortification(new WorldFortificationId("missing")),
                new WorldUnitId("leader"),
                GridPosition.BackBottom),
            WorldCommandError.GarrisonNotFound);

        engine.CheckCommandError(
            new WorldRepositionUnitCommand(
                new WorldUnitGroup.Fortification(enemyCity.Id),
                new WorldUnitId("smallUnit"),
                GridPosition.BackBottom),
            WorldCommandError.GarrisonNotFound);

        engine.CheckCommandError(
            new WorldRepositionUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitId("missing"),
                GridPosition.BackCenter),
            WorldCommandError.UnitNotFound);

        engine.CheckCommandError(
            new WorldRepositionUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitId("smallUnit"),
                new GridPosition(0, 3)),
            WorldCommandError.InvalidUnitPosition);
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
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("frontSoldier"), frontSoldierDefinition), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("backSoldier"), backSoldierDefinition), GridPosition.BackCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontTop, true)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(frontSoldierDefinition)
            .WithDefinition(backSoldierDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldRepositionUnitCommand(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("large"), GridPosition.FrontCenter),
            [
                new WorldUnitRepositionedEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("large"), GridPosition.FrontCenter)
            ]);

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("large"), largeDefinition), GridPosition.FrontCenter, true)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("frontSoldier"), frontSoldierDefinition), GridPosition.FrontTop, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("backSoldier"), backSoldierDefinition), GridPosition.BackTop, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = expectedGarrison
                })
            }));
    }
}
