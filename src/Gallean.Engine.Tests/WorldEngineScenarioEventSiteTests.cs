using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineScenarioEventSiteTests
{
    [Test]
    public void VisitingSiteMoveTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldMerchantSite site = new(
            new WorldSiteId("Site1"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            new ScenarioTextId("Title"),
            new ScenarioTextId("Greeting"),
            [],
            VisitorIds: [],
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventVisitingSiteCondition(site.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Welcome, traveler!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithSite(site)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(4, 4)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 4, [], null),
                new WorldSiteVisitedEvent(stack.Id, site.Id),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Welcome, traveler!"), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(4, 4), [player.Id]),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Sites = ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty.Add(site.Id, site with { VisitorIds = [player.Id] }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void VisitingSiteMoveOutsideTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

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

        WorldMerchantSite site = new(
            new WorldSiteId("Site1"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            new ScenarioTextId("Title"),
            new ScenarioTextId("Greeting"),
            [],
            VisitorIds: [],
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventVisitingSiteCondition(site.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Welcome, traveler!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithSite(site)
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
                Sites = ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty.Add(site.Id, site),
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void VisitingSiteStandingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldMerchantSite site = new(
            new WorldSiteId("Site1"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            new ScenarioTextId("Title"),
            new ScenarioTextId("Greeting"),
            [],
            VisitorIds: [],
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventVisitingSiteCondition(site.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Welcome, traveler!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithSite(site)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Sites = ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty.Add(site.Id, site),
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void VisitingSiteLeftTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldMerchantSite site = new(
            new WorldSiteId("Site1"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            new ScenarioTextId("Title"),
            new ScenarioTextId("Greeting"),
            [],
            VisitorIds: [],
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [
                new ScenarioEventVisitingSiteCondition(site.Id),
                new ScenarioEventFrequencyCondition(1)
            ],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Welcome, traveler!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithSite(site)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(4, 4)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 4, [], null),
                new WorldSiteVisitedEvent(stack.Id, site.Id)
            ]);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(5, 5)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(4, 4), new WorldPosition(5, 5), WorldStackDirection.South, 2, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(4, 4), new WorldPosition(5, 5), WorldStackDirection.South, 2, [], null)
            ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Welcome, traveler!"), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Sites = ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty.Add(site.Id, site with { VisitorIds = [player.Id] }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void VisitingSiteOtherPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

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

        WorldMerchantSite site = new(
            new WorldSiteId("Site1"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            new ScenarioTextId("Title"),
            new ScenarioTextId("Greeting"),
            [],
            VisitorIds: [enemy.Id],
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventVisitingSiteCondition(site.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Welcome, traveler!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithSite(site)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Sites = ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty.Add(site.Id, site),
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void VisitingSiteAffectedRaceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer ally = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack allyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            ally.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldMerchantSite site = new(
            new WorldSiteId("Site1"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            new ScenarioTextId("Title"),
            new ScenarioTextId("Greeting"),
            [],
            VisitorIds: [],
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human, WorldRace.Dwarf],
            [WorldRace.Human],
            [new ScenarioEventVisitingSiteCondition(site.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Welcome, traveler!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(ally.LordId, ally.Race, LordCategory.Warrior))
            .WithPlayer(ally)
            .WithStack(stack)
            .WithStack(allyStack)
            .WithSite(site)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(allyStack.Id, new WorldPosition(4, 4)),
            [
                new WorldStackMovingEvent(allyStack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 4, [], null),
                new WorldStackMovedEvent(allyStack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 4, [], null),
                new WorldSiteVisitedEvent(allyStack.Id, site.Id)
            ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(ally.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Welcome, traveler!"), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Sites = ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty.Add(site.Id, site with { VisitorIds = [ally.Id] }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void VisitingSiteRevisitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldMerchantSite site = new(
            new WorldSiteId("Site1"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            new ScenarioTextId("Title"),
            new ScenarioTextId("Greeting"),
            [],
            VisitorIds: [player.Id],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(4, 4)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 4, [], null),
                new WorldSiteVisitedEvent(stack.Id, site.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Sites = ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty.Add(site.Id, site),
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void VisitingSiteDeadLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 10, initiative: 10, damage: 10);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 0 }, GridPosition.FrontBottom, leader.IsLarge)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontCenter, soldier.IsLarge)
                .Build(),
            movementPoints: 6);

        WorldMerchantSite site = new(
            new WorldSiteId("Site1"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            new ScenarioTextId("Title"),
            new ScenarioTextId("Greeting"),
            [],
            VisitorIds: [],
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventVisitingSiteCondition(site.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Welcome, traveler!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSite(site)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(4, 4)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 2, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 2, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Sites = ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty.Add(site.Id, site),
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void VisitingSiteThiefTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldMerchantSite site = new(
            new WorldSiteId("Site1"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            new ScenarioTextId("Title"),
            new ScenarioTextId("Greeting"),
            [],
            VisitorIds: [],
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventVisitingSiteCondition(site.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Welcome, traveler!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSite(site)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(4, 4)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 18, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 18, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Sites = ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty.Add(site.Id, site),
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void VisitingSiteTriggerEventTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldMerchantSite site = new(
            new WorldSiteId("Site1"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            new ScenarioTextId("Title"),
            new ScenarioTextId("Greeting"),
            [],
            VisitorIds: [],
            AiPriority: 3);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(4, 4), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [],
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
            .WithSite(site)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(4, 4)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Sites = ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty.Add(site.Id, site),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }
}
