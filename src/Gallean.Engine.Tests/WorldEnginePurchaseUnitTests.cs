namespace Gallean.Engine.Tests;

public sealed class WorldEnginePurchaseUnitTests
{
    [Test]
    public void SuccessTest()
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
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent)
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPurchaseUnitCommand(capital.Id, unitDefinition.Id, GridPosition.FrontBottom),
            [
                new WorldUnitPurchasedEvent(capital.Id, TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), unitDefinition), GridPosition.FrontBottom, new WorldResourceSet(50, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
                }),
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
                {
                    Garrison = TestHelpers.SingleUnitGrid(new WorldUnitId("Unit0001"), unitDefinition)
                })
            }));
    }

    [Test]
    public void LargeUnitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("large_unit", maxHitpoints: 200, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("large_unit"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent),
            IsLarge = true,
            PurchaseCost = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(200, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPurchaseUnitCommand(capital.Id, unitDefinition.Id, GridPosition.FrontBottom),
            [
                new WorldUnitPurchasedEvent(capital.Id, TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), unitDefinition), GridPosition.FrontBottom, new WorldResourceSet(150, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
                }),
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
                {
                    Garrison = TestHelpers.SingleUnitGrid(new WorldUnitId("Unit0001"), unitDefinition)
                })
            }));
    }

    [Test]
    public void FortificationNotFoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent)
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseUnitCommand(new WorldFortificationId("missing"), unitDefinition.Id, GridPosition.FrontBottom),
            WorldCommandError.FortificationNotFound
        );
    }

    [Test]
    public void OtherPlayerCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent)
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldPlayer other = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            other.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(other.LordId, other.Race, LordCategory.Warrior))
            .WithPlayer(other)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseUnitCommand(capital.Id, unitDefinition.Id, GridPosition.FrontBottom),
            WorldCommandError.FortificationNotFound
        );
    }

    [Test]
    public void UnitNotAvailableTest()
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
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent)
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseUnitCommand(capital.Id, unitDefinition.Id, GridPosition.FrontBottom),
            WorldCommandError.UnitNotAvailable
        );
    }

    [Test]
    public void InsufficientResourcesTest()
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
            PurchaseCost = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseUnitCommand(capital.Id, unitDefinition.Id, GridPosition.FrontBottom),
            WorldCommandError.InsufficientResources
        );
    }

    [Test]
    public void MissingPrerequisiteTest()
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
            RequiredBuildingId = new WorldPlayerBuildingDefinitionId("building")
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseUnitCommand(capital.Id, unitDefinition.Id, GridPosition.FrontBottom),
            WorldCommandError.UnitMissingPrerequisite
        );
    }

    [Test]
    public void LargeUnitBackLineTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("large_unit", maxHitpoints: 200, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("large_unit"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent),
            IsLarge = true,
            PurchaseCost = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(200, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPurchaseUnitCommand(capital.Id, unitDefinition.Id, GridPosition.BackBottom),
            [
                new WorldUnitPurchasedEvent(capital.Id, TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), unitDefinition), GridPosition.BackBottom, new WorldResourceSet(150, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
                }),
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
                {
                    Garrison = TestHelpers.SingleUnitGrid(new WorldUnitId("Unit0001"), unitDefinition)
                })
            }));
    }

    [Test]
    public void TargetOccupiedTest()
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
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent)
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Hitpoints = 10 }, GridPosition.FrontBottom, unitDefinition.IsLarge)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseUnitCommand(capital.Id, unitDefinition.Id, GridPosition.FrontBottom),
            WorldCommandError.TargetOccupied
        );
    }

    [Test]
    public void OutOfGridPositionTest()
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
            PurchaseCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(200, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseUnitCommand(capital.Id, unitDefinition.Id, new GridPosition(0, 3)),
            WorldCommandError.InvalidUnitPosition
        );
    }

    [Test]
    public void CityGarrisonTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(cityDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPurchaseUnitCommand(city.Id, unitDefinition.Id, GridPosition.FrontBottom),
            [
                new WorldUnitPurchasedEvent(city.Id, TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), unitDefinition), GridPosition.FrontBottom, new WorldResourceSet(50, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = TestHelpers.SingleUnitGrid(new WorldUnitId("Unit0001"), unitDefinition)
                })
            }));
    }

    [Test]
    public void CityCapacityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(cityDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseUnitCommand(city.Id, unitDefinition.Id, GridPosition.FrontCenter),
            WorldCommandError.GarrisonCapacityExceeded
        );
    }

    [Test]
    public void RiotingCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10);

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 2),
            new WorldResourceSet(150, 0, 0, 0, 0, 0));

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                RiotTurns = 3
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(cityDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseUnitCommand(city.Id, unitDefinition.Id, GridPosition.FrontBottom),
            WorldCommandError.CityRioting
        );
    }

    [Test]
    public void UnaffordableMissingPrerequisiteTest()
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
            PurchaseCost = new WorldResourceSet(150, 0, 0, 0, 0, 0),
            RequiredBuildingId = new WorldPlayerBuildingDefinitionId("building")
        };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: null,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseUnitCommand(capital.Id, unitDefinition.Id, GridPosition.FrontBottom),
            WorldCommandError.UnitMissingPrerequisite
        );
    }
}
