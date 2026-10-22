namespace Gallean.Engine.Tests;

public sealed class WorldEngineTransferInventoryItemStackTests
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
    public void TransferTest()
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

        ItemDefinitionId potion = new("potion");
        ItemDefinitionId scroll = new("scroll");
        WorldItem potionItem = new(new WorldItemId("Potion"), potion, Charges: null);
        WorldItem scrollItem = new(new WorldItemId("Scroll"), scroll, Charges: null);

        WorldStack sourceStack = Stack(new WorldUnitId("stack1Leader"), "stack1", player, new WorldPosition(3, 3), leaderDefinition, new WorldInventory([potionItem, scrollItem]));
        WorldStack targetStack = Stack(new WorldUnitId("stack2Leader"), "stack2", player, new WorldPosition(4, 3), leaderDefinition, WorldInventory.Empty);

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
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), potionItem.Id),
            [new WorldInventoryItemTransferredEvent(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(targetStack.Id), potionItem)]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(sourceStack.Id, sourceStack with
                    {
                        MovementPoints = 10,
                        Inventory = new WorldInventory([scrollItem])
                    })
                    .SetItem(targetStack.Id, targetStack with
                    {
                        MovementPoints = 10,
                        Inventory = new WorldInventory([potionItem])
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

        WorldPlayer otherPlayer = player with
        {
            Id = new WorldPlayerId(2)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        ItemDefinitionId potion = new("potion");
        WorldItem potionItem = new(new WorldItemId("Potion"), potion, Charges: null);

        WorldStack sourceStack = Stack(new WorldUnitId("stack1Leader"), "stack1", player, new WorldPosition(3, 3), leaderDefinition, new WorldInventory([potionItem]));
        WorldStack targetStack = Stack(new WorldUnitId("stack2Leader"), "stack2", player, new WorldPosition(4, 3), leaderDefinition, WorldInventory.Empty);
        WorldStack enemyStack = Stack(new WorldUnitId("stack3Leader"), "stack3", otherPlayer, new WorldPosition(2, 3), leaderDefinition, WorldInventory.Empty);
        WorldStack farStack = Stack(new WorldUnitId("stack4Leader"), "stack4", player, new WorldPosition(1, 1), leaderDefinition, WorldInventory.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithStack(sourceStack)
            .WithStack(targetStack)
            .WithStack(enemyStack)
            .WithStack(farStack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(new WorldStackId("missing")), new WorldUnitGroup.Stack(targetStack.Id), potionItem.Id),
            WorldCommandError.StackNotFound);

        engine.CheckCommandError(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(new WorldStackId("missing")), potionItem.Id),
            WorldCommandError.StackNotFound);

        engine.CheckCommandError(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(enemyStack.Id), new WorldUnitGroup.Stack(sourceStack.Id), potionItem.Id),
            WorldCommandError.StackNotFound);

        engine.CheckCommandError(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(enemyStack.Id), potionItem.Id),
            WorldCommandError.StackNotFound);

        engine.CheckCommandError(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(sourceStack.Id), new WorldUnitGroup.Stack(farStack.Id), potionItem.Id),
            WorldCommandError.StackNotAdjacent);

        engine.CheckCommandError(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(targetStack.Id), new WorldUnitGroup.Stack(sourceStack.Id), potionItem.Id),
            WorldCommandError.ItemNotFound);
    }
}
