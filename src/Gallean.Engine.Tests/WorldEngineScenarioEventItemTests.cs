using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineScenarioEventItemTests
{
    [Test]
    public void ItemToLocationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(2, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Altar"),
            new WorldPosition(0, 0),
            1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventItemToLocationCondition(item, location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is home."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 1)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.North, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.North, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The relic is home."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(1, 1), [player.Id]),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The relic is home."),
                            ScenarioEventPortraitId.Unit(leader.Id),
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void ItemToLocationOtherItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(2, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Potion"), new ItemDefinitionId("Potion"), Charges: null)])
            };

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Altar"),
            new WorldPosition(0, 0),
            1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventItemToLocationCondition(item, location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is home."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 1)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.North, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.North, 4, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void ItemToLocationOutsideTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Altar"),
            new WorldPosition(2, 2),
            0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventItemToLocationCondition(item, location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is home."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void ItemToLocationOtherPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer rival = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack rivalStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                rival.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Altar"),
            new WorldPosition(0, 0),
            1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventItemToLocationCondition(item, location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is home."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(rival.LordId, rival.Race, LordCategory.Warrior))
            .WithPlayer(rival)
            .WithStack(stack)
            .WithStack(rivalStack)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void ItemToLocationTurnStartTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 1),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Altar"),
            new WorldPosition(0, 1),
            0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventItemToLocationCondition(item, location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is home."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void DestroyItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition itemDefinition = new(
            new ItemDefinitionId("Relic"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Armor,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        ItemDefinitionId item = itemDefinition.Id;
        ItemDefinitionId potion = new("Potion");

        WorldItem stackPotion = new(new WorldItemId("StackPotion"), potion, Charges: null);
        WorldItem capitalPotion = new(new WorldItemId("CapitalPotion"), potion, Charges: null);
        WorldItem keptBagPotion = new(new WorldItemId("KeptBagPotion"), potion, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([stackPotion, new WorldItem(new WorldItemId("StackRelic"), item, Charges: null), new WorldItem(new WorldItemId("SecondStackRelic"), item, Charges: null)]),
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, new WorldItem(new WorldItemId("EquippedRelic"), item, Charges: null))
            };

        WorldCapital capital = TestHelpers.SimpleCapital(
                new WorldFortificationId("Capital"),
                player.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("CapitalRelic"), item, Charges: null), capitalPotion])
            };

        WorldBag emptiedBag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(2, 0),
            new WorldObjectVariant(0),
            new WorldInventory([new WorldItem(new WorldItemId("EmptiedBagRelic"), item, Charges: null)]),
            AiPriority: 0);

        WorldBag keptBag = new(
            new WorldBagId("Bag2"),
            new WorldPosition(3, 0),
            new WorldObjectVariant(0),
            new WorldInventory([new WorldItem(new WorldItemId("KeptBagRelic"), item, Charges: null), keptBagPotion]),
            AiPriority: 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventDestroyItemEffect(item, TriggererOnly: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithCapital(capital)
            .WithBag(emptiedBag)
            .WithBag(keptBag)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(
                    stack.Id,
                    stack with { Inventory = new WorldInventory([stackPotion]), Equipment = LeaderEquipment.Empty }),
                Fortifications = engine.State.Fortifications.SetItem(
                    capital.Id,
                    capital with { Inventory = new WorldInventory([capitalPotion]) }),
                Bags = engine.State.Bags
                    .Remove(emptiedBag.Id)
                    .SetItem(keptBag.Id, keptBag with { Inventory = new WorldInventory([keptBagPotion]) }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void DestroyItemMissingItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId potion = new("Potion");

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Potion"), potion, Charges: null)])
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventDestroyItemEffect(new ItemDefinitionId("Relic"), TriggererOnly: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void DestroyItemTriggererOnlyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(2, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        WorldStack otherStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                player.Id,
                new WorldPosition(2, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("OtherRelic"), item, Charges: null)])
            };

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Altar"),
            new WorldPosition(0, 0),
            1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventItemToLocationCondition(item, location.Id)],
            [new ScenarioEventDestroyItemEffect(item, TriggererOnly: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 1)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.North, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.North, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack.Id, stack with
                    {
                        Position = new WorldPosition(1, 1),
                        Direction = WorldStackDirection.North,
                        MovementPoints = 4,
                        Inventory = WorldInventory.Empty
                    })
                    .SetItem(otherStack.Id, otherStack),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void DestroyItemTriggererOnlyTurnStartTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventDestroyItemEffect(item, TriggererOnly: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void DestroyItemBeforeGiveItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        ItemDefinition itemDefinition = new(
            new ItemDefinitionId("Relic"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Special,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        ItemDefinitionId item = itemDefinition.Id;

        WorldCapital capital = TestHelpers.SimpleCapital(
                new WorldFortificationId("Capital"),
                player.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventDestroyItemEffect(item, TriggererOnly: false),
                new ScenarioEventGiveItemEffect(item, ScenarioEventGiveItemTarget.TriggererCapital)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with { Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), item, Charges: null)]) }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void OwningItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventOwningItemCondition(item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is ours."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The relic is ours."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void OwningItemInCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        ItemDefinitionId item = new("Relic");

        WorldCapital capital = TestHelpers.SimpleCapital(
                new WorldFortificationId("Capital"),
                player.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventOwningItemCondition(item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is ours."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The relic is ours."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void OwningItemOtherAffectedRaceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer rival = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack rivalStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                rival.Id,
                new WorldPosition(2, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human, WorldRace.Undead],
            [WorldRace.Human, WorldRace.Undead],
            [new ScenarioEventOwningItemCondition(item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic has been found."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(rival.LordId, rival.Race, LordCategory.Warrior))
            .WithPlayer(rival)
            .WithStack(stack)
            .WithStack(rivalStack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The relic has been found."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id]),
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The relic has been found."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            rival.Id,
                            [rival.Id])
                    ]
                }
            }));
    }

    [Test]
    public void OwningItemUnaffectedRaceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer rival = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack rivalStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                rival.Id,
                new WorldPosition(2, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventOwningItemCondition(item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is ours."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(rival.LordId, rival.Race, LordCategory.Warrior))
            .WithPlayer(rival)
            .WithStack(stack)
            .WithStack(rivalStack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void OwningItemInBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(2, 2),
            new WorldObjectVariant(0),
            new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)]),
            AiPriority: 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventOwningItemCondition(item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is ours."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void LeaderOwningItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventLeaderOwningItemCondition(new ScenarioStackReference.Stack(stack.Id), item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The bearer is ready."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The bearer is ready."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void LeaderOwningItemOtherStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack bearer = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                player.Id,
                new WorldPosition(2, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventLeaderOwningItemCondition(new ScenarioStackReference.Stack(stack.Id), item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The bearer is ready."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithStack(bearer)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void LeaderOwningItemTemplateInstanceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack plain = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        WorldStack bearer = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                player.Id,
                new WorldPosition(2, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)]),
                SourceTemplateId = templateId
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventLeaderOwningItemCondition(new ScenarioStackReference.Template(templateId), item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The bearer is ready."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(plain)
            .WithStack(bearer)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The bearer is ready."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void OwningItemPickedUpTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(0, 1),
            new WorldObjectVariant(0),
            new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)]),
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventOwningItemCondition(item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is ours."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(0, 1)),
            [
                new WorldBagPickedUpEvent(stack.Id, bag.Id, bag.Inventory),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventItemGainedTrigger(new WorldUnitGroup.Stack(stack.Id))),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The relic is ours."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(0, 0), [player.Id]),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventItemGainedTrigger(new WorldUnitGroup.Stack(stack.Id)))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The relic is ours."),
                            ScenarioEventPortraitId.Unit(leader.Id),
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void OwningItemPickedUpAfterStepTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(0, 0),
            new WorldObjectVariant(0),
            new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)]),
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventOwningItemCondition(item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is ours."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(0, 0)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.North, 4, [], new WorldPosition(0, 0)),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.North, 4, [], new WorldPosition(0, 0)),
                new WorldBagPickedUpEvent(stack.Id, bag.Id, bag.Inventory),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventItemGainedTrigger(new WorldUnitGroup.Stack(stack.Id))),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The relic is ours."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(1, 1), [player.Id]),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventItemGainedTrigger(new WorldUnitGroup.Stack(stack.Id)))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The relic is ours."),
                            ScenarioEventPortraitId.Unit(leader.Id),
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void LeaderOwningItemTransferredTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack giver = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        WorldStack bearer = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player.Id,
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventLeaderOwningItemCondition(new ScenarioStackReference.Stack(bearer.Id), item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The bearer is ready."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(giver)
            .WithStack(bearer)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferInventoryItemCommand(new WorldUnitGroup.Stack(giver.Id), new WorldUnitGroup.Stack(bearer.Id), new WorldItemId("Relic")),
            [
                new WorldInventoryItemTransferredEvent(new WorldUnitGroup.Stack(giver.Id), new WorldUnitGroup.Stack(bearer.Id), new WorldItem(new WorldItemId("Relic"), item, Charges: null)),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventItemGainedTrigger(new WorldUnitGroup.Stack(bearer.Id))),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The bearer is ready."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(0, 1), [player.Id]),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventItemGainedTrigger(new WorldUnitGroup.Stack(bearer.Id)))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The bearer is ready."),
                            ScenarioEventPortraitId.Unit(leader.Id),
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void ItemToLocationEquippedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition itemDefinition = new(
            new ItemDefinitionId("Relic"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Armor,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(2, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, new WorldItem(new WorldItemId("Relic"), itemDefinition.Id, Charges: null))
            };

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Altar"),
            new WorldPosition(0, 0),
            1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventItemToLocationCondition(itemDefinition.Id, location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is home."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 1)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.North, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.North, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The relic is home."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(1, 1), [player.Id]),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The relic is home."),
                            ScenarioEventPortraitId.Unit(leader.Id),
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void OwningItemEquippedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer rival = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition itemDefinition = new(
            new ItemDefinitionId("Relic"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Armor,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack rivalStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                rival.Id,
                new WorldPosition(2, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 6) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, new WorldItem(new WorldItemId("Relic"), itemDefinition.Id, Charges: null))
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Undead],
            [WorldRace.Human],
            [new ScenarioEventOwningItemCondition(itemDefinition.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The enemy has the relic."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(rival.LordId, rival.Race, LordCategory.Warrior))
            .WithPlayer(rival)
            .WithStack(stack)
            .WithStack(rivalStack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The enemy has the relic."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void LeaderOwningItemEquippedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition itemDefinition = new(
            new ItemDefinitionId("Relic"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Armor,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, new WorldItem(new WorldItemId("Relic"), itemDefinition.Id, Charges: null))
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventLeaderOwningItemCondition(new ScenarioStackReference.Stack(stack.Id), itemDefinition.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The bearer is ready."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The bearer is ready."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void ItemToLocationCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(4, 3),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Altar"),
            new WorldPosition(3, 3),
            0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventItemToLocationCondition(item, location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is home."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(3, 3)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(4, 3), new WorldPosition(3, 3), WorldStackDirection.NorthWest, 6, [], null),
                new WorldFortificationEnteredEvent(stack.Id, city.Id),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(4, 3), new WorldPosition(3, 3), WorldStackDirection.NorthWest, 6, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void OwningItemCaptureCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(11, 5);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer bystander = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Dwarf, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            attacker.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);

        WorldCapital capital = TestHelpers.SimpleCapital(
                new WorldFortificationId("Capital1"),
                defender.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };
        WorldCapital bystanderCapital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital2"),
            bystander.Id,
            new WorldPosition(10, 4),
            UnitGrid<WorldUnit>.Empty);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventOwningItemCondition(item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is ours."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithDefinition(TestHelpers.SimpleLord(bystander.LordId, bystander.Race, LordCategory.Warrior))
            .WithPlayer(bystander)
            .WithStack(stack)
            .WithCapital(capital)
            .WithCapital(bystanderCapital)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, capital.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("Stack1"), new WorldFortificationId("Capital1")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldScenarioEventTriggeringEvent(
                scenarioEvent.Id,
                attacker.Id,
                new ScenarioEventFortificationCapturedTrigger(capital.Id, stack.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The relic is ours."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, attacker.Id, TriggererPosition: new WorldPosition(4, 4), [attacker.Id]),
            new WorldScenarioEventTriggeredEvent(
                scenarioEvent.Id,
                attacker.Id,
                new ScenarioEventFortificationCapturedTrigger(capital.Id, stack.Id)),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = capital.Entrance,
                    Direction = WorldStackDirection.NorthWest,
                    MovementPoints = 2,
                    Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
                }),
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
                {
                    Inventory = WorldInventory.Empty,
                    CapturedById = attacker.Id
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The relic is ours."),
                            ScenarioEventPortraitId.Unit(leader.Id),
                            PortraitName: null,
                            IsPortraitLeft: true,
                            attacker.Id,
                            [attacker.Id])
                    ]
                }
            }));
    }

    [Test]
    public void OwningItemKillTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        ItemDefinitionId item = new("Relic");

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack enemyStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                enemy.Id,
                new WorldPosition(1, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leader), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)])
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventOwningItemCondition(item)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The relic is ours."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(enemyStack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleUnit survivingUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("leader"), leader.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(
            new WorldResolveStackBattleCommand(stack.Id, enemyStack.Id, finalBattleState, IsDuel: false),
            [
                new WorldStackBattleEndedEvent(stack.Id, enemyStack.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
                new WorldStackDestroyedEvent(enemyStack.Id, new WorldUnitGroup.Stack(stack.Id), stack.OwnerId),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id))),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The relic is ours."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(0, 0), [player.Id]),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id)))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(stack.Id, stack with
                {
                    Units = expectedAttackerGrid,
                    Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), item, Charges: null)]),
                    BattlesWon = 1
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The relic is ours."),
                            ScenarioEventPortraitId.Unit(leader.Id),
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ],
                    DestroyedStacks = [new ScenarioDestroyedStack(enemyStack.Id, player.Id, SourceTemplateId: null)]
                }
            }));
    }
}
