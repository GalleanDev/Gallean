using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiBagTests
{
    [Test]
    public void NormalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
            movementPoints: 10, leadership: 6);
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(4, 2),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = WorldStackOrder.Normal.Instance
            };

        WorldBag bag = new(
            Id: new WorldBagId("Bag"),
            Position: new WorldPosition(8, 7),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithBag(bag)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(1, player1.Id, 1, 0, 0, []))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 1000),
            new WorldAiMoveStackCommand(moverStack.Id, bag.Position),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(4, 2), new WorldPosition(4, 3), WorldStackDirection.SouthWest, 8, [new WorldPosition(5, 4), new WorldPosition(6, 5), new WorldPosition(7, 6)], bag.Position),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(4, 2), new WorldPosition(4, 3), WorldStackDirection.SouthWest, 8, [new WorldPosition(5, 4), new WorldPosition(6, 5), new WorldPosition(7, 6)], bag.Position),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(4, 3), new WorldPosition(5, 4), WorldStackDirection.South, 6, [new WorldPosition(6, 5), new WorldPosition(7, 6)], bag.Position),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(4, 3), new WorldPosition(5, 4), WorldStackDirection.South, 6, [new WorldPosition(6, 5), new WorldPosition(7, 6)], bag.Position),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(5, 4), new WorldPosition(6, 5), WorldStackDirection.South, 4, [new WorldPosition(7, 6)], bag.Position),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(5, 4), new WorldPosition(6, 5), WorldStackDirection.South, 4, [new WorldPosition(7, 6)], bag.Position),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(6, 5), new WorldPosition(7, 6), WorldStackDirection.South, 2, [], bag.Position),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(6, 5), new WorldPosition(7, 6), WorldStackDirection.South, 2, [], bag.Position),
            new WorldBagPickedUpEvent(moverStack.Id, bag.Id, new WorldInventory([bagItem])),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void RoamTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
            movementPoints: 10, leadership: 6);
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(4, 2),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = WorldStackOrder.Roam.Instance
            };

        WorldBag bag = new(
            Id: new WorldBagId("Bag"),
            Position: new WorldPosition(7, 5),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithBag(bag)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, player1.Id, 1, 0, 0, []));

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(1, 600),
            new WorldAiMoveStackCommand(moverStack.Id, bag.Position),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(4, 2), new WorldPosition(5, 3), WorldStackDirection.South, 8, [new WorldPosition(6, 4)], bag.Position),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(4, 2), new WorldPosition(5, 3), WorldStackDirection.South, 8, [new WorldPosition(6, 4)], bag.Position),
            new WorldStackMovingEvent(moverStack.Id, new WorldPosition(5, 3), new WorldPosition(6, 4), WorldStackDirection.South, 6, [], bag.Position),
            new WorldStackMovedEvent(moverStack.Id, new WorldPosition(5, 3), new WorldPosition(6, 4), WorldStackDirection.South, 6, [], bag.Position),
            new WorldBagPickedUpEvent(moverStack.Id, bag.Id, new WorldInventory([bagItem])),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void FarRoamTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
            movementPoints: 10, leadership: 6);
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(4, 2),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = WorldStackOrder.Roam.Instance
            };

        WorldBag bag = new(
            Id: new WorldBagId("Bag"),
            Position: new WorldPosition(8, 5),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithBag(bag)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, player1.Id, 0, 0, 0, [moverStack.Id]));

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(WorldAiActionPlan.EndTurn(memory)),
            new WorldTurnEndedEvent(player1.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player2.Id),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void BerserkTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
            movementPoints: 10, leadership: 6);
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(4, 2),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = WorldStackOrder.Berserk.Instance
            };

        WorldBag bag = new(
            Id: new WorldBagId("Bag"),
            Position: new WorldPosition(6, 2),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithBag(bag)
            .Build();

        WorldAiMemory memory = new(new WorldAiTurnMemory(1, player1.Id, 0, 0, 0, [moverStack.Id]));

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(WorldAiActionPlan.EndTurn(memory)),
            new WorldTurnEndedEvent(player1.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player2.Id),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void ScenarioItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
            movementPoints: 10, leadership: 6);
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(4, 2),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = WorldStackOrder.Normal.Instance
            };

        WorldBag bag = new(
            Id: new WorldBagId("Bag"),
            Position: new WorldPosition(8, 7),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithBag(bag)
            .WithScenarioEvent(new ScenarioEventDefinition(
                new ScenarioEventId("Victory"),
                [WorldRace.Heretic],
                [WorldRace.Heretic],
                [new ScenarioEventOwningItemCondition(item)],
                [new ScenarioEventWinEffect(new WorldPlayerId(2))],
                Enabled: true,
                OccurOnce: true,
                Chance: 100,
                Order: 0))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(
                new WorldAiTurnMemory(1, player1.Id, 0, 0, 0, [moverStack.Id]))
            .WithStackMemory(moverStack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(WorldAiActionPlan.EndTurn(memory)),
            new WorldTurnEndedEvent(player1.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player2.Id),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldAiActionEndedEvent(player1.Id, memory)
        ]);
    }

    [Test]
    public void SummonLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
                movementPoints: 10, leadership: 6) with
            {
                Category = UnitCategory.Summon
            };
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(4, 2),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = WorldStackOrder.Roam.Instance
            };

        WorldBag bag = new(
            Id: new WorldBagId("Bag"),
            Position: new WorldPosition(7, 5),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithBag(bag)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[moverStack.Id], Is.EqualTo(moverStack));
        Assert.That(session.World.State.Bags[bag.Id], Is.EqualTo(bag));
    }

    [Test]
    public void IllusionLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
                movementPoints: 10, leadership: 6) with
            {
                Category = UnitCategory.Illusion
            };
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(4, 2),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = WorldStackOrder.Roam.Instance
            };

        WorldBag bag = new(
            Id: new WorldBagId("Bag"),
            Position: new WorldPosition(7, 5),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithBag(bag)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[moverStack.Id], Is.EqualTo(moverStack));
        Assert.That(session.World.State.Bags[bag.Id], Is.EqualTo(bag));
    }

    [Test]
    public void RodLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition mover = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("mover", maxHitpoints: 100, initiative: 40, damage: 50),
                movementPoints: 10, leadership: 6) with
            {
                LeaderCategory = UnitLeaderCategory.Rod
            };
        ItemDefinitionId item = new("item");
        WorldItem bagItem = new(new WorldItemId("BagItem"), item, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack moverStack = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                player1.Id,
                new WorldPosition(4, 2),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("mover"), mover), GridPosition.FrontBottom, false).Build(),
                movementPoints: 10) with
            {
                Order = WorldStackOrder.Roam.Instance
            };

        WorldBag bag = new(
            Id: new WorldBagId("Bag"),
            Position: new WorldPosition(7, 5),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([bagItem]),
            AiPriority: 3);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mover)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(moverStack)
            .WithBag(bag)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[moverStack.Id], Is.EqualTo(moverStack));
        Assert.That(session.World.State.Bags[bag.Id], Is.EqualTo(bag));
    }
}
