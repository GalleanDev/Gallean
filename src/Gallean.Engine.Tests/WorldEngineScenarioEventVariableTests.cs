using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineScenarioEventVariableTests
{
    [Test]
    public void InRangeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [
                new ScenarioEventVariableInRangeCondition(
                    new ScenarioVariableRange(new ScenarioVariableId(1), 2, 4),
                    null)
            ],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The seal is broken."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(1), "SEAL", 3))
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The seal is broken."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
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
                            new ScenarioTextId("The seal is broken."),
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
    public void OutOfRangeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [
                new ScenarioEventVariableInRangeCondition(
                    new ScenarioVariableRange(new ScenarioVariableId(1), 4, 6),
                    null)
            ],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The seal is broken."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(1), "SEAL", 3))
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
    public void AndTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [
                new ScenarioEventVariableInRangeCondition(
                    new ScenarioVariableRange(new ScenarioVariableId(1), 1, 1),
                    new ScenarioVariableClause(
                        ScenarioVariableOperator.And,
                        new ScenarioVariableRange(new ScenarioVariableId(2), 1, 1)))
            ],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Both walls are sealed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(1), "WALL2", 1))
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(2), "WALL3", 1))
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Both walls are sealed."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
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
                            new ScenarioTextId("Both walls are sealed."),
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
    public void AndMismatchTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [
                new ScenarioEventVariableInRangeCondition(
                    new ScenarioVariableRange(new ScenarioVariableId(1), 1, 1),
                    new ScenarioVariableClause(
                        ScenarioVariableOperator.And,
                        new ScenarioVariableRange(new ScenarioVariableId(2), 1, 1)))
            ],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Both walls are sealed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(1), "WALL2", 1))
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(2), "WALL3", 0))
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
    public void OrTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [
                new ScenarioEventVariableInRangeCondition(
                    new ScenarioVariableRange(new ScenarioVariableId(1), 1, 1),
                    new ScenarioVariableClause(
                        ScenarioVariableOperator.Or,
                        new ScenarioVariableRange(new ScenarioVariableId(2), 1, 1)))
            ],
            [new ScenarioEventPopupEffect(new ScenarioTextId("A wall is sealed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(1), "WALL2", 0))
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(2), "WALL3", 1))
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("A wall is sealed."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
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
                            new ScenarioTextId("A wall is sealed."),
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
    public void AddTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventModifyVariableEffect(
                    new ScenarioVariableId(1),
                    ScenarioVariableOperation.Add,
                    2)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(1), "TALE", 3))
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Variables = ImmutableSortedDictionary<ScenarioVariableId, int>.Empty
                        .Add(new ScenarioVariableId(1), 5)
                }
            }));
    }

    [Test]
    public void SubtractTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventModifyVariableEffect(
                    new ScenarioVariableId(1),
                    ScenarioVariableOperation.Subtract,
                    5)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(1), "TALE", 3))
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Variables = ImmutableSortedDictionary<ScenarioVariableId, int>.Empty
                        .Add(new ScenarioVariableId(1), -2)
                }
            }));
    }

    [Test]
    public void MultiplyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventModifyVariableEffect(
                    new ScenarioVariableId(1),
                    ScenarioVariableOperation.Multiply,
                    4)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(1), "TALE", 3))
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Variables = ImmutableSortedDictionary<ScenarioVariableId, int>.Empty
                        .Add(new ScenarioVariableId(1), 12)
                }
            }));
    }

    [Test]
    public void DivideTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventModifyVariableEffect(
                    new ScenarioVariableId(1),
                    ScenarioVariableOperation.Divide,
                    2)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(1), "TALE", 7))
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Variables = ImmutableSortedDictionary<ScenarioVariableId, int>.Empty
                        .Add(new ScenarioVariableId(1), 3)
                }
            }));
    }

    [Test]
    public void ReplaceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventModifyVariableEffect(
                    new ScenarioVariableId(1),
                    ScenarioVariableOperation.Replace,
                    9)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(1), "TALE", 3))
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Variables = ImmutableSortedDictionary<ScenarioVariableId, int>.Empty
                        .Add(new ScenarioVariableId(1), 9)
                }
            }));
    }

    [Test]
    public void GatesLaterEventTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition tickingEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventModifyVariableEffect(
                    new ScenarioVariableId(1),
                    ScenarioVariableOperation.Add,
                    1)
            ],
            Enabled: true,
            OccurOnce: false,
            Chance: 100,
            Order: 0);
        ScenarioEventDefinition gatedEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human],
            [WorldRace.Human],
            [
                new ScenarioEventVariableInRangeCondition(
                    new ScenarioVariableRange(new ScenarioVariableId(1), 1, 1),
                    null)
            ],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The tale begins."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioVariable(new ScenarioVariableDefinition(new ScenarioVariableId(1), "TALE", 0))
            .WithScenarioEvent(tickingEvent)
            .WithScenarioEvent(gatedEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(tickingEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(tickingEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeringEvent(gatedEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(gatedEvent.Id, new ScenarioTextId("The tale begins."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(gatedEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(tickingEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(tickingEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(gatedEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            gatedEvent.Id,
                            new ScenarioTextId("The tale begins."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ],
                    Variables = ImmutableSortedDictionary<ScenarioVariableId, int>.Empty
                        .Add(new ScenarioVariableId(1), 2)
                }
            }));
    }
}
