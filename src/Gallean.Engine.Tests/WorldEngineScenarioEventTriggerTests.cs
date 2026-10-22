using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineScenarioEventTriggerTests
{
    [Test]
    public void EventFiresOnTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Greetings Milord!"), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void EventSkipsOtherTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(2)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void TurnZeroTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition everyTurn = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Every turn"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);
        ScenarioEventDefinition everyThirdTurn = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(3)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Every third turn"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 1);
        ScenarioEventDefinition stackAbsent = new(
            new ScenarioEventId("Event3"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Stack(new WorldStackId("Missing")), MustExist: false)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The Bone Lord was destroyed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(0, new WorldPlayerId(1)))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(everyTurn)
            .WithScenarioEvent(everyThirdTurn)
            .WithScenarioEvent(stackAbsent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void EventSkipsOtherRaceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Dwarf],
            [WorldRace.Dwarf],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void AiTurnTriggersEventTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with { IsAi = true };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Greetings Milord!"), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void AiMoveTriggersEventTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with { IsAi = true };
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
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CaptureTriggersEventTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            attacker.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            defender.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventOwningCityCondition(city.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The city is ours!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCity(city)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(4, 3), city.Entrance, WorldStackDirection.NorthWest, 2, [], null),
                new WorldFortificationEnteredEvent(new WorldStackId("Stack1"), new WorldFortificationId("City1")),
                new WorldFortificationCapturingEvent(city.Id, defender.Id, attacker.Id, stack.Id),
                new WorldFortificationCapturedEvent(city.Id, defender.Id, attacker.Id, stack.Id),
                new WorldScenarioEventTriggeringEvent(
                    scenarioEvent.Id,
                    attacker.Id,
                    new ScenarioEventFortificationCapturedTrigger(city.Id, stack.Id)),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The city is ours!"), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, attacker.Id, TriggererPosition: new WorldPosition(3, 3), [attacker.Id]),
                new WorldScenarioEventTriggeredEvent(
                    scenarioEvent.Id,
                    attacker.Id,
                    new ScenarioEventFortificationCapturedTrigger(city.Id, stack.Id)),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(4, 3), city.Entrance, WorldStackDirection.NorthWest, 2, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = city.Entrance,
                    Direction = WorldStackDirection.NorthWest
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    OwnerId = attacker.Id
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChanceRollFiresEventTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 30,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .WithRandom(new TestSequenceRandomProvider(29))
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Greetings Milord!"), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChanceRollSkipsEventTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 30,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .WithRandom(new TestSequenceRandomProvider(30))
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void ChanceRollKeepsWalkingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition unlikely = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Unlikely"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 30,
            Order: 0);
        ScenarioEventDefinition certain = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Certain"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(unlikely)
            .WithScenarioEvent(certain)
            .WithRandom(new TestSequenceRandomProvider(30, 30))
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(certain.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(certain.Id, new ScenarioTextId("Certain"), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(certain.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(certain.Id, false)
                }
            }));
    }

    [Test]
    public void CertainEventRollsNothingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .WithRandom(new TestSequenceRandomProvider())
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Greetings Milord!"), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void VerifiedRaceTriggersTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Undead],
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Stack(new WorldStackId("Missing")), MustExist: false)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The Bone Lord was destroyed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, enemy.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The Bone Lord was destroyed."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, enemy.Id, TriggererPosition: null, [enemy.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, enemy.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void FrequencyOtherPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Undead],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void VerifiedRaceNotPlayingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Undead],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void EveryVerifiedPlayerTriggersTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human, WorldRace.Dwarf, WorldRace.Undead],
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Stack(new WorldStackId("Missing")), MustExist: false)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The Bone Lord was destroyed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The Bone Lord was destroyed."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, enemy.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The Bone Lord was destroyed."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, enemy.Id, TriggererPosition: null, [enemy.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, enemy.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void NeutralTurnTriggersEventTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map) with { IsAi = true };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Neutral],
            [WorldRace.Neutral],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The dead stir."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 0, 0, 25))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player.Id),
            new WorldTurnStartingEvent(neutral.Id),
            new WorldTurnStartedEvent(neutral.Id),
            new WorldTurnResourcesGeneratedEvent(neutral.Id, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, neutral.Id, new ScenarioEventTurnStartedTrigger(neutral.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The dead stir."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, neutral.Id, TriggererPosition: null, [neutral.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, neutral.Id, new ScenarioEventTurnStartedTrigger(neutral.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }
}
