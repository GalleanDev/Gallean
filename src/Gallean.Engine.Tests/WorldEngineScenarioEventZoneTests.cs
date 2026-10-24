using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineScenarioEventZoneTests
{
    [Test]
    public void EnteringZoneMoveTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Ambush!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Ambush!"), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(0, 1), [player.Id]),
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
                            new ScenarioTextId("Ambush!"),
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
    public void EnteringZoneCornerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 0), 1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Ambush!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 1)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.North, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.North, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Ambush!"), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(1, 1), [player.Id]),
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
                            new ScenarioTextId("Ambush!"),
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
    public void EnteringZoneMoveOutsideTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(2, 2), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Ambush!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithScenarioLocation(zone)
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
    public void EnteringZoneMidPathStopsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(2, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Ambush!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(4, 0)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 8, [new WorldPosition(2, 0), new WorldPosition(3, 0), new WorldPosition(4, 0)], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 8, [new WorldPosition(2, 0), new WorldPosition(3, 0), new WorldPosition(4, 0)], null),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 6, [new WorldPosition(3, 0), new WorldPosition(4, 0)], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 6, [new WorldPosition(3, 0), new WorldPosition(4, 0)], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Ambush!"), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(2, 0), [player.Id]),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(2, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 6
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("Ambush!"),
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
    public void EnteringZoneTurnStartTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Ambush!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithScenarioLocation(zone)
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
    public void EnteringZoneOnPickedUpBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        ItemDefinition relic = new(
            new ItemDefinitionId("relic"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Valuable,
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

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(0, 1),
            new WorldObjectVariant(0),
            new WorldInventory([new WorldItem(new WorldItemId("Relic"), relic.Id, Charges: null)]),
            AiPriority: 3);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Stack(stack.Id), FirstOnly: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(relic)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPickUpBagCommand(stack.Id, bag.Id),
            [
                new WorldBagPickedUpEvent(stack.Id, bag.Id, bag.Inventory)
            ]);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldStackRemovedEvent(stack.Id),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Bags = engine.State.Bags.Remove(bag.Id),
                Stacks = engine.State.Stacks.Remove(stack.Id),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(stack.Id, DestroyerPlayerId: null, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void EnteringZoneUnverifiedRaceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Undead],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Ambush!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithScenarioLocation(zone)
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
    public void EnteringZoneUnaffectedRaceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Undead],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Ambush!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithScenarioLocation(zone)
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
    public void EnteringZoneCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

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
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Gate"), new WorldPosition(3, 3), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Ambush!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithScenarioLocation(zone)
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
    public void EnteringZoneLeavingCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

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
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Gate"), new WorldPosition(4, 4), 1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Ambush!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(4, 4)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(3, 3), new WorldPosition(4, 4), WorldStackDirection.South, 6, [], null),
                new WorldFortificationExitedEvent(stack.Id, city.Id),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(3, 3), new WorldPosition(4, 4), WorldStackDirection.South, 6, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Ambush!"), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(4, 4), [player.Id]),
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
                            new ScenarioTextId("Ambush!"),
                            ScenarioEventPortraitId.Unit(leader.Id),
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }
}
