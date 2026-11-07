namespace Gallean.Engine.Tests;

public sealed class WorldEngineStepStackTests
{
    [Test]
    public void SimpleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
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

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldStepStackCommand(stack.Id, new WorldPosition(3, 0)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(1, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 4
                })
            }));
    }

    [Test]
    public void DestinationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
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

        WorldState before = engine.State;

        _ = engine.DispatchCommand(new WorldStepStackCommand(stack.Id, new WorldPosition(3, 0)));
        _ = engine.DispatchCommand(new WorldStepStackCommand(stack.Id, new WorldPosition(3, 0)));

        engine.CheckCommand(
            new WorldStepStackCommand(stack.Id, new WorldPosition(3, 0)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(2, 0), new WorldPosition(3, 0), WorldStackDirection.SouthEast, 0, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(2, 0), new WorldPosition(3, 0), WorldStackDirection.SouthEast, 0, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(3, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 0
                })
            }));
    }

    [Test]
    public void BagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldBag bag = new(
            Id: new WorldBagId("bag"),
            Position: new WorldPosition(3, 0),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldStepStackCommand(stack.Id, bag.Position),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], null)
            ]);

        engine.CheckCommand(
            new WorldStepStackCommand(stack.Id, bag.Position),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 2, [], bag.Position),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 2, [], bag.Position),
                new WorldBagPickedUpEvent(stack.Id, bag.Id, new WorldInventory([bagItem]))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(2, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 2,
                    Inventory = new WorldInventory([bagItem])
                }),
                Bags = before.Bags.Remove(bag.Id)
            }));
    }
}
