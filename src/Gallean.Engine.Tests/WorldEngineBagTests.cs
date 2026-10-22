using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineBagTests
{
    private static ItemDefinition Item(string id)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), ItemCategory.Valuable);
    }

    [Test]
    public void PickUpBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition carriedItem = Item("carriedItem");
        ItemDefinition firstItem = Item("firstItem");
        ItemDefinition secondItem = Item("secondItem");
        WorldItem carriedWorldItem = new(new WorldItemId("Carried"), carriedItem.Id, Charges: null);
        WorldItem firstWorldItem = new(new WorldItemId("First"), firstItem.Id, Charges: null);
        WorldItem secondWorldItem = new(new WorldItemId("Second"), secondItem.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([carriedWorldItem])
            };

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(1, 0),
            new WorldObjectVariant(0),
            new WorldInventory([firstWorldItem, secondWorldItem]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(carriedItem)
            .WithDefinition(firstItem)
            .WithDefinition(secondItem)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPickUpBagCommand(stack.Id, bag.Id),
            [
                new WorldBagPickedUpEvent(stack.Id, bag.Id, new WorldInventory([firstWorldItem, secondWorldItem]))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = new WorldInventory([
                        carriedWorldItem,
                        firstWorldItem,
                        secondWorldItem
                    ])
                }),
                Bags = engine.State.Bags.Remove(bag.Id)
            }));
    }

    [Test]
    public void PickUpDistantBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition item = Item("item");
        WorldItem worldItem = new(new WorldItemId("Item"), item.Id, Charges: null);

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
            new WorldInventory([worldItem]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(item)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPickUpBagCommand(stack.Id, bag.Id),
            WorldCommandError.TargetTooFar);
    }

    [Test]
    public void PickUpMissingBagTest()
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

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPickUpBagCommand(stack.Id, new WorldBagId("Bag1")),
            WorldCommandError.BagNotFound);
    }

    [Test]
    public void PickUpBagOtherPlayerStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer firstPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer secondPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition item = Item("item");
        WorldItem worldItem = new(new WorldItemId("Item"), item.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            secondPlayer.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(1, 0),
            new WorldObjectVariant(0),
            new WorldInventory([worldItem]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(item)
            .WithDefinition(TestHelpers.SimpleLord(firstPlayer.LordId, firstPlayer.Race, LordCategory.Warrior))
            .WithPlayer(firstPlayer)
            .WithDefinition(TestHelpers.SimpleLord(secondPlayer.LordId, secondPlayer.Race, LordCategory.Warrior))
            .WithPlayer(secondPlayer)
            .WithStack(stack)
            .WithBag(bag)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPickUpBagCommand(stack.Id, bag.Id),
            WorldCommandError.StackNotFound);
    }

    [Test]
    public void DropItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition droppedItem = Item("droppedItem");
        ItemDefinition keptItem = Item("keptItem");
        WorldItem droppedWorldItem = new(new WorldItemId("Dropped"), droppedItem.Id, Charges: null);
        WorldItem keptWorldItem = new(new WorldItemId("Kept"), keptItem.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([droppedWorldItem, keptWorldItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(droppedItem)
            .WithDefinition(keptItem)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldDropItemCommand(stack.Id, new WorldPosition(1, 0), droppedWorldItem.Id),
            [
                new WorldItemDroppedEvent(stack.Id, new WorldBagId("Bag0001"), new WorldPosition(1, 0), droppedWorldItem)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = new WorldInventory([keptWorldItem])
                }),
                Bags = engine.State.Bags.SetItem(new WorldBagId("Bag0001"), new WorldBag(
                    new WorldBagId("Bag0001"),
                    new WorldPosition(1, 0),
                    new WorldObjectVariant(0),
                    new WorldInventory([droppedWorldItem]),
                    AiPriority: 3))
            }));
    }

    [Test]
    public void DropItemIntoExistingBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition droppedItem = Item("droppedItem");
        ItemDefinition lyingItem = Item("lyingItem");
        WorldItem droppedWorldItem = new(new WorldItemId("Dropped"), droppedItem.Id, Charges: 5);
        WorldItem lyingWorldItem = new(new WorldItemId("Lying"), lyingItem.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([droppedWorldItem])
            };

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(1, 0),
            new WorldObjectVariant(2),
            new WorldInventory([lyingWorldItem]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(droppedItem)
            .WithDefinition(lyingItem)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldDropItemCommand(stack.Id, new WorldPosition(1, 0), droppedWorldItem.Id),
            [
                new WorldItemDroppedEvent(stack.Id, bag.Id, new WorldPosition(1, 0), droppedWorldItem)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty
                }),
                Bags = engine.State.Bags.SetItem(bag.Id, bag with
                {
                    Inventory = new WorldInventory([lyingWorldItem, droppedWorldItem])
                })
            }));
    }

    [Test]
    public void DropItemOnOwnTileTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition item = Item("item");
        WorldItem worldItem = new(new WorldItemId("Item"), item.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([worldItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(item)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldDropItemCommand(stack.Id, stack.Position, worldItem.Id),
            WorldCommandError.TargetOccupied);
    }

    [Test]
    public void DropItemOnMountainTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "..^"
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition item = Item("item");
        WorldItem worldItem = new(new WorldItemId("Item"), item.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(1, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([worldItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(item)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldDropItemCommand(stack.Id, new WorldPosition(2, 0), worldItem.Id),
            WorldCommandError.TargetUnpassable);
    }

    [Test]
    public void DropItemTooFarTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition item = Item("item");
        WorldItem worldItem = new(new WorldItemId("Item"), item.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([worldItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(item)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldDropItemCommand(stack.Id, new WorldPosition(2, 0), worldItem.Id),
            WorldCommandError.TargetTooFar);
    }

    [Test]
    public void DropMissingItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition item = Item("item");
        WorldItem worldItem = new(new WorldItemId("Item"), item.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(item)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldDropItemCommand(stack.Id, new WorldPosition(1, 0), worldItem.Id),
            WorldCommandError.ItemNotFound);
    }

    [Test]
    public void DropItemOnTombTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition item = Item("item");
        WorldItem worldItem = new(new WorldItemId("Item"), item.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([worldItem])
            };

        WorldTomb tomb = new(
            new WorldTombId("Tomb0001"),
            new WorldPosition(1, 0),
            player.Id,
            leader.Id,
            LeaderName: null,
            DestroyerPlayerId: null,
            DestroyerLeaderDefinitionId: null,
            DestroyerLeaderName: null,
            Turn: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(item)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithTomb(tomb)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldDropItemCommand(stack.Id, new WorldPosition(1, 0), worldItem.Id),
            WorldCommandError.TargetOccupied);
    }

    [Test]
    public void DropItemIntoBagOnTombTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition droppedItem = Item("droppedItem");
        ItemDefinition lyingItem = Item("lyingItem");
        WorldItem droppedWorldItem = new(new WorldItemId("Dropped"), droppedItem.Id, Charges: null);
        WorldItem lyingWorldItem = new(new WorldItemId("Lying"), lyingItem.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([droppedWorldItem])
            };

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(1, 0),
            new WorldObjectVariant(0),
            new WorldInventory([lyingWorldItem]),
            AiPriority: 3);

        WorldTomb tomb = new(
            new WorldTombId("Tomb0001"),
            new WorldPosition(1, 0),
            player.Id,
            leader.Id,
            LeaderName: null,
            DestroyerPlayerId: null,
            DestroyerLeaderDefinitionId: null,
            DestroyerLeaderName: null,
            Turn: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(droppedItem)
            .WithDefinition(lyingItem)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .WithTomb(tomb)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldDropItemCommand(stack.Id, new WorldPosition(1, 0), droppedWorldItem.Id),
            [
                new WorldItemDroppedEvent(stack.Id, bag.Id, new WorldPosition(1, 0), droppedWorldItem)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty
                }),
                Bags = engine.State.Bags.SetItem(bag.Id, bag with
                {
                    Inventory = new WorldInventory([lyingWorldItem, droppedWorldItem])
                }),
                Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty.Add(tomb.Id, tomb)
            }));
    }
}
