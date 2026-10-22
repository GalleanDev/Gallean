namespace Gallean.Engine.Tests;

public sealed class WorldEngineAutoFitStackTests
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
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("leader"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Any),
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 2
        };
        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("ranged", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("ranged"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Any)
        };
        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 13, 1, [], null, false);
        WorldUnit unit = new(new WorldUnitId("unit"), unitDefinition.Id, unitDefinition.MaxHitpoints, 13, 1, [], null, false);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(unit, GridPosition.FrontBottom, false)
                .Build());
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(unitDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> expectedStack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.BackCenter, false)
            .Place(unit, GridPosition.BackBottom, false)
            .Build();

        engine.CheckCommand(
            new WorldAutoFitStackCommand(stack.Id),
            [new WorldStackAutoFittedEvent(stack.Id, expectedStack, UnitGrid<WorldUnit>.Empty, WorldInventory.Empty, WorldInventory.Empty, LeaderEquipment.Empty)]
        );

        WorldState expectedState = engine.State with
        {
            Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
            {
                Garrison = UnitGrid<WorldUnit>.Empty
            }),
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                MovementPoints = 0,
                Units = expectedStack
            })
        };

        Assert.That(engine.State, Is.EqualTo(expectedState));
    }

    [Test]
    public void HitpointsTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("leader"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Any),
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 2
        };
        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("ranged", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("ranged"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Any)
        };
        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 13, 1, [], null, false);
        WorldUnit unit = new(new WorldUnitId("unit"), unitDefinition.Id, 30, 13, 1, [], null, false);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(unit, GridPosition.FrontBottom, false)
                .Build());
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(unitDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> expectedStack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, GridPosition.BackCenter, false)
            .Place(unit with { Hitpoints = 45 }, GridPosition.BackBottom, false)
            .Build();

        engine.CheckCommand(
            new WorldAutoFitStackCommand(stack.Id),
            [new WorldStackAutoFittedEvent(stack.Id, expectedStack, UnitGrid<WorldUnit>.Empty, WorldInventory.Empty, WorldInventory.Empty, LeaderEquipment.Empty)]
        );

        WorldState expectedState = engine.State with
        {
            Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
            {
                Garrison = UnitGrid<WorldUnit>.Empty
            }),
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                MovementPoints = 0,
                Units = expectedStack
            })
        };

        Assert.That(engine.State, Is.EqualTo(expectedState));
    }

    [Test]
    public void LeaderSwapTest()
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
            Leadership = 2,
            PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition specialDefinition = TestHelpers.SimpleUnitDefinition("special", maxHitpoints: 200, initiative: 40, damage: 20) with
        {
            Leadership = 6,
            PurchaseCost = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 30, damage: 5) with
        {
            PurchaseCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, 1, [], null, false);
        WorldUnit special = new(new WorldUnitId("special"), specialDefinition.Id, 200, 0, 1, [], null, false);
        WorldUnit soldier1 = new(new WorldUnitId("soldier1"), soldierDefinition.Id, 50, 0, 1, [], null, false);
        WorldUnit soldier2 = new(new WorldUnitId("soldier2"), soldierDefinition.Id, 50, 0, 1, [], null, false);
        WorldUnit soldier3 = new(new WorldUnitId("soldier3"), soldierDefinition.Id, 50, 0, 1, [], null, false);
        WorldUnit soldier4 = new(new WorldUnitId("soldier4"), soldierDefinition.Id, 50, 0, 1, [], null, false);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(special, GridPosition.FrontBottom, false)
                .Place(soldier1, GridPosition.FrontCenter, false)
                .Place(soldier2, GridPosition.FrontTop, false)
                .Place(soldier3, GridPosition.BackBottom, false)
                .Place(soldier4, GridPosition.BackCenter, false)
                .Build());
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(specialDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> expectedStack = new UnitGrid<WorldUnit>.Builder()
            .Place(soldier4, new GridPosition(0, 1), false)
            .Place(special, new GridPosition(0, 0), false)
            .Place(soldier1, new GridPosition(1, 1), false)
            .Place(soldier2, new GridPosition(1, 0), false)
            .Place(soldier3, new GridPosition(1, 2), false)
            .Build();

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(leader, new GridPosition(1, 1), false)
            .Build();

        engine.CheckCommand(
            new WorldAutoFitStackCommand(stack.Id),
            [new WorldStackAutoFittedEvent(stack.Id, expectedStack, expectedGarrison, WorldInventory.Empty, WorldInventory.Empty, LeaderEquipment.Empty)]
        );

        WorldState expectedState = engine.State with
        {
            Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
            {
                Garrison = expectedGarrison
            }),
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                MovementPoints = 0,
                Units = expectedStack
            })
        };

        Assert.That(engine.State, Is.EqualTo(expectedState));
    }

    [Test]
    public void InventoryItemsTest()
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
            Leadership = 2,
            LeaderAbilities = LeaderAbility.WeaponArmorUse
        };
        UnitModifierDefinition weaponModifier = TestHelpers.SimpleModifier("weapon-mod") with
        {
            Damage = new UnitStatModifier(20, 0)
        };
        ItemDefinition weapon = new(
            new ItemDefinitionId("weapon"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Weapon,
            new WorldResourceSet(300, 0, 0, 0, 0, 0),
            EquipmentModifierId: weaponModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 13, 1, [], null, false);
        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                player.Id,
                city.Entrance,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([weaponItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(weaponModifier)
            .WithDefinition(weapon)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> expectedStack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader with { Modifiers = [UnitModifier.FromEquipment(weaponModifier.Id)] }, GridPosition.BackCenter, false)
            .Build();
        LeaderEquipment expectedEquipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, weaponItem);

        engine.CheckCommand(
            new WorldAutoFitStackCommand(stack.Id),
            [new WorldStackAutoFittedEvent(stack.Id, expectedStack, UnitGrid<WorldUnit>.Empty, WorldInventory.Empty, WorldInventory.Empty, expectedEquipment)]
        );

        WorldState expectedState = engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                MovementPoints = 0,
                Units = expectedStack,
                Inventory = WorldInventory.Empty,
                Equipment = expectedEquipment
            })
        };

        Assert.That(engine.State, Is.EqualTo(expectedState));
    }

    [Test]
    public void GarrisonInventoryItemsTest()
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
            Leadership = 2,
            LeaderAbilities = LeaderAbility.WeaponArmorUse
        };
        UnitModifierDefinition weaponModifier = TestHelpers.SimpleModifier("weapon-mod") with
        {
            Damage = new UnitStatModifier(20, 0)
        };
        ItemDefinition weapon = new(
            new ItemDefinitionId("weapon"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Weapon,
            new WorldResourceSet(300, 0, 0, 0, 0, 0),
            EquipmentModifierId: weaponModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 13, 1, [], null, false);
        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
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
                Inventory = new WorldInventory([weaponItem])
            };
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            city.Entrance,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(weaponModifier)
            .WithDefinition(weapon)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> expectedStack = new UnitGrid<WorldUnit>.Builder()
            .Place(leader with { Modifiers = [UnitModifier.FromEquipment(weaponModifier.Id)] }, GridPosition.BackCenter, false)
            .Build();
        LeaderEquipment expectedEquipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, weaponItem);

        engine.CheckCommand(
            new WorldAutoFitStackCommand(stack.Id),
            [new WorldStackAutoFittedEvent(stack.Id, expectedStack, UnitGrid<WorldUnit>.Empty, WorldInventory.Empty, WorldInventory.Empty, expectedEquipment)]
        );

        WorldState expectedState = engine.State with
        {
            Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
            {
                Inventory = WorldInventory.Empty
            }),
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                MovementPoints = 0,
                Units = expectedStack,
                Equipment = expectedEquipment
            })
        };

        Assert.That(engine.State, Is.EqualTo(expectedState));
    }

    [Test]
    public void MissingGarrisonTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(0, 0),
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
            new WorldAutoFitStackCommand(stack.Id),
            WorldCommandError.GarrisonNotFound
        );
    }

    [Test]
    public void RiotTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);
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
            new WorldStackId("stack"),
            player.Id,
            city.Entrance,
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 20);

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

        engine.CheckCommandError(
            new WorldAutoFitStackCommand(stack.Id),
            WorldCommandError.CityRioting
        );
    }
}
