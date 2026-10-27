namespace Gallean.Engine.Tests;

public sealed class WorldEngineTransferInventoryTests
{
    private static WorldStack Stack(
        WorldUnitId leaderId,
        string id,
        WorldPlayer owner,
        WorldPosition position,
        UnitDefinition leaderDefinition,
        WorldInventory inventory)
    {
        return TestHelpers.SimpleStack(
                new WorldStackId(id),
                owner.Id,
                position,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(leaderId, leaderDefinition), new GridPosition(0, 0), false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = inventory
            };
    }

    [Test]
    public void StackToStackTest()
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

        WorldItem potionItem = new(new WorldItemId("Potion"), new ItemDefinitionId("potion"), Charges: null);
        WorldItem scrollItem = new(new WorldItemId("Scroll"), new ItemDefinitionId("scroll"), Charges: null);
        WorldItem orbItem = new(new WorldItemId("Orb"), new ItemDefinitionId("orb"), Charges: null);

        WorldStack sourceStack = Stack(new WorldUnitId("stack1Leader"), "stack1", player, new WorldPosition(3, 3), leaderDefinition, new WorldInventory([potionItem, scrollItem]));
        WorldStack targetStack = Stack(new WorldUnitId("stack2Leader"), "stack2", player, new WorldPosition(4, 3), leaderDefinition, new WorldInventory([orbItem]));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(sourceStack)
            .WithStack(targetStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferInventoryCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id)),
            [
                new WorldInventoryItemTransferredEvent(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), potionItem),
                new WorldInventoryItemTransferredEvent(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), scrollItem)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(sourceStack.Id, sourceStack with
                    {
                        MovementPoints = 10,
                        Inventory = WorldInventory.Empty
                    })
                    .SetItem(targetStack.Id, targetStack with
                    {
                        MovementPoints = 10,
                        Inventory = new WorldInventory([orbItem, potionItem, scrollItem])
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

        WorldItem potionItem = new(new WorldItemId("Potion"), new ItemDefinitionId("potion"), Charges: null);
        WorldItem scrollItem = new(new WorldItemId("Scroll"), new ItemDefinitionId("scroll"), Charges: null);

        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city1"),
                cityDefinition,
                player.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([potionItem, scrollItem])
            };

        WorldStack stack = Stack(new WorldUnitId("leader"), "stack1", player, new WorldPosition(3, 3), leaderDefinition, WorldInventory.Empty);

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
            new WorldTransferInventoryCommand(new WorldUnitGroup.Fortification(city.Id), new WorldUnitGroup.Stack(stack.Id)),
            [
                new WorldInventoryItemTransferredEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitGroup.Stack(stack.Id), potionItem),
                new WorldInventoryItemTransferredEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitGroup.Stack(stack.Id), scrollItem)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 10,
                    Inventory = new WorldInventory([potionItem, scrollItem])
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Inventory = WorldInventory.Empty
                })
            }));
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

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldItem potionItem = new(new WorldItemId("Potion"), new ItemDefinitionId("potion"), Charges: null);

        WorldStack sourceStack = Stack(new WorldUnitId("stack1Leader"), "stack1", player, new WorldPosition(3, 3), leaderDefinition, new WorldInventory([potionItem]));
        WorldStack targetStack = Stack(new WorldUnitId("stack2Leader"), "stack2", player, new WorldPosition(4, 3), leaderDefinition, WorldInventory.Empty);
        WorldStack farStack = Stack(new WorldUnitId("stack3Leader"), "stack3", player, new WorldPosition(1, 1), leaderDefinition, WorldInventory.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(sourceStack)
            .WithStack(targetStack)
            .WithStack(farStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferInventoryCommand(new WorldUnitGroup.Stack(new WorldStackId("missing")), new WorldUnitGroup.Stack(targetStack.Id)),
            WorldCommandError.StackNotFound);

        engine.CheckCommandError(
            new WorldTransferInventoryCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(farStack.Id)),
            WorldCommandError.StackNotAdjacent);

        engine.CheckCommandError(
            new WorldTransferInventoryCommand(new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitGroup.Stack(sourceStack.Id)),
            WorldCommandError.ItemNotFound);
    }
}
