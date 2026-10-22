namespace Gallean.Engine.Tests;

public sealed class WorldEngineTransferInventoryItemGarrisonTests
{
    [Test]
    public void StackToCityTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldItem potion = new(new WorldItemId("Potion"), new ItemDefinitionId("potion"), Charges: null);

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
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potion])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitGroup.Fortification(city.Id), potion.Id),
            [new WorldInventoryItemTransferredEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitGroup.Fortification(city.Id), potion)]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 10,
                    Inventory = WorldInventory.Empty
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Inventory = new WorldInventory([potion])
                })
            }));
    }

    [Test]
    public void CityToStackTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldItem potion = new(new WorldItemId("Potion"), new ItemDefinitionId("potion"), Charges: null);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city1"),
                cityDefinition,
                player.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([potion])
            };

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
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Fortification(city.Id), new WorldUnitGroup.Stack(stack.Id), potion.Id),
            [new WorldInventoryItemTransferredEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitGroup.Stack(stack.Id), potion)]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 10,
                    Inventory = new WorldInventory([potion])
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Inventory = WorldInventory.Empty
                })
            }));
    }

    [Test]
    public void StackToCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldItem potion = new(new WorldItemId("Potion"), new ItemDefinitionId("potion"), Charges: null);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital1"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack1"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potion])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitGroup.Fortification(capital.Id), potion.Id),
            [new WorldInventoryItemTransferredEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitGroup.Fortification(capital.Id), potion)]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 10,
                    Inventory = WorldInventory.Empty
                }),
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
                {
                    Inventory = new WorldInventory([potion])
                })
            }));
    }

    [Test]
    public void StackNotInCityTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldItem potion = new(new WorldItemId("Potion"), new ItemDefinitionId("potion"), Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack1"),
                player.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potion])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitGroup.Fortification(new WorldFortificationId("missing")), potion.Id),
            WorldCommandError.StackNotInCity);
    }

    [Test]
    public void OtherPlayerCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldItem potion = new(new WorldItemId("Potion"), new ItemDefinitionId("potion"), Charges: null);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital2"),
            otherPlayer.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack1"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potion])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithCapital(capital)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitGroup.Fortification(capital.Id), potion.Id),
            WorldCommandError.StackNotInCity);
    }

    [Test]
    public void ItemNotFoundTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

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
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(0, 0), false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitGroup.Fortification(city.Id), new WorldItemId("Potion")),
            WorldCommandError.ItemNotFound);
    }
}
