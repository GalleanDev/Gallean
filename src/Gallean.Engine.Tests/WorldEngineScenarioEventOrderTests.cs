using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineScenarioEventOrderTests
{
    [Test]
    public void AttackEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldSubrace subrace = new(new WorldSubraceId("human"), player.Id, WorldSubraceType.Human, new WorldObjectVariant(1));
        WorldSubrace enemySubrace = new(new WorldSubraceId("undead"), enemyPlayer.Id, WorldSubraceType.Undead, new WorldObjectVariant(2));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SubraceId = subrace.Id
            };
        WorldStack enemy = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                enemyPlayer.Id,
                new WorldPosition(4, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("enemyLeader"), leader),
                movementPoints: 6) with
            {
                SubraceId = enemySubrace.Id
            };
        WorldCanonicalStackOrder order = new WorldStackOrder.AttackStack(enemy.Id);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventChangeOrderEffect(new ScenarioStackReference.Stack(stack.Id), order, FirstOnly: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithSubrace(subrace)
            .WithSubrace(enemySubrace)
            .WithStack(stack)
            .WithStack(enemy)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOrderChangedEvent(stack.Id, order),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with { Order = order }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void AttackSameSubraceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldSubrace subrace = new(new WorldSubraceId("human"), player.Id, WorldSubraceType.Human, new WorldObjectVariant(1));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SubraceId = subrace.Id
            };
        WorldStack ally = stack with
        {
            Id = new WorldStackId("Stack2"),
            Position = new WorldPosition(4, 0),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("allyLeader"), leader)
        };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    new WorldStackOrder.AttackStack(ally.Id),
                    FirstOnly: true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(subrace)
            .WithStack(stack)
            .WithStack(ally)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldStackMovementRestoredEvent(ally.Id, 6),
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
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void AttackSameNeutralSubraceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

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
        WorldStack ally = stack with
        {
            Id = new WorldStackId("Stack2"),
            Position = new WorldPosition(4, 0),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("allyLeader"), leader)
        };
        WorldCanonicalStackOrder order = new WorldStackOrder.AttackStack(ally.Id);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventChangeOrderEffect(new ScenarioStackReference.Stack(stack.Id), order, FirstOnly: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithStack(ally)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldStackMovementRestoredEvent(ally.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOrderChangedEvent(stack.Id, order),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with { Order = order }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void AttackSelfTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

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

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    new WorldStackOrder.AttackStack(stack.Id),
                    FirstOnly: true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
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
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void AttackMissingTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Order = WorldStackOrder.Guard.Instance
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    new WorldStackOrder.AttackStack(new WorldStackId("Missing")),
                    FirstOnly: true)
            ],
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

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
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
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void DefendAllyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldSubrace subrace = new(new WorldSubraceId("human"), player.Id, WorldSubraceType.Human, new WorldObjectVariant(1));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SubraceId = subrace.Id
            };
        WorldStack ally = stack with
        {
            Id = new WorldStackId("Stack2"),
            Position = new WorldPosition(4, 0),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("allyLeader"), leader)
        };
        WorldCanonicalStackOrder order = new WorldStackOrder.DefendStack(ally.Id);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventChangeOrderEffect(new ScenarioStackReference.Stack(stack.Id), order, FirstOnly: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(subrace)
            .WithStack(stack)
            .WithStack(ally)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldStackMovementRestoredEvent(ally.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOrderChangedEvent(stack.Id, order),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with { Order = order }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void DefendEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldSubrace subrace = new(new WorldSubraceId("human"), player.Id, WorldSubraceType.Human, new WorldObjectVariant(1));
        WorldSubrace enemySubrace = new(new WorldSubraceId("undead"), enemyPlayer.Id, WorldSubraceType.Undead, new WorldObjectVariant(2));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SubraceId = subrace.Id
            };
        WorldStack enemy = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                enemyPlayer.Id,
                new WorldPosition(4, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("enemyLeader"), leader),
                movementPoints: 6) with
            {
                SubraceId = enemySubrace.Id
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    new WorldStackOrder.DefendStack(enemy.Id),
                    FirstOnly: true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithSubrace(subrace)
            .WithSubrace(enemySubrace)
            .WithStack(stack)
            .WithStack(enemy)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
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
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void AssistAllyWithOrderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldSubrace subrace = new(new WorldSubraceId("human"), player.Id, WorldSubraceType.Human, new WorldObjectVariant(1));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SubraceId = subrace.Id
            };
        WorldStack ally = stack with
        {
            Id = new WorldStackId("Stack2"),
            Position = new WorldPosition(4, 0),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("allyLeader"), leader),
            Order = WorldStackOrder.Stand.Instance
        };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    new WorldStackOrder.Assist(ally.Id),
                    FirstOnly: true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(subrace)
            .WithStack(stack)
            .WithStack(ally)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldStackMovementRestoredEvent(ally.Id, 6),
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
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void NormalNeutralOwnerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);
        WorldSubrace neutralSubrace = new(new WorldSubraceId("neutralHuman"), neutral.Id, WorldSubraceType.NeutralHuman, new WorldObjectVariant(6));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                neutral.Id,
                new WorldPosition(4, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SubraceId = neutralSubrace.Id,
                Order = WorldStackOrder.Berserk.Instance
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    WorldStackOrder.Normal.Instance,
                    FirstOnly: true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithPlayer(neutral)
            .WithSubrace(neutralSubrace)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOrderChangedEvent(stack.Id, WorldStackOrder.Stand.Instance),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with { Order = WorldStackOrder.Stand.Instance }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void SecureCityNeutralSubraceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldCapital capital = TestHelpers.SimpleCapital(new WorldFortificationId("Capital"), player.Id, new WorldPosition(4, 4), UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    new WorldStackOrder.SecureCity(capital.Id),
                    FirstOnly: true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithCapital(capital)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOrderChangedEvent(stack.Id, WorldStackOrder.Stand.Instance),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with { Order = WorldStackOrder.Stand.Instance }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CreateStackAttackMissingTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldSubrace subrace = new(new WorldSubraceId("human"), player.Id, WorldSubraceType.Human, new WorldObjectVariant(1));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        ScenarioStackTemplateDefinition template = new(
            templateId,
            player.Id,
            subrace.Id,
            TestHelpers.SingleTemplateUnitGrid(leader),
            new WorldStackOrder.AttackStack(new WorldStackId("Missing")),
            Direction: null,
            AiPriority: 4);

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Camp"), new ScenarioTextId("Camp"), new WorldPosition(4, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventCreateStackEffect(templateId, location.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(subrace)
            .WithScenarioLocation(location)
            .WithScenarioStackTemplate(template)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldStack created = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                new WorldPosition(4, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                SubraceId = subrace.Id,
                AiPriority = 4,
                Order = WorldStackOrder.Berserk.Instance,
                SourceTemplateId = templateId
            };

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackCreatedEvent(created, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(created.Id, created),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CreateStackRejectedOrderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldSubrace subrace = new(new WorldSubraceId("human"), player.Id, WorldSubraceType.Human, new WorldObjectVariant(1));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        ScenarioStackTemplateDefinition template = new(
            templateId,
            player.Id,
            subrace.Id,
            TestHelpers.SingleTemplateUnitGrid(leader),
            new WorldStackOrder.DefendStack(new WorldStackId("Missing")),
            Direction: null,
            AiPriority: 4);

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Camp"), new ScenarioTextId("Camp"), new WorldPosition(4, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventCreateStackEffect(templateId, location.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(subrace)
            .WithScenarioLocation(location)
            .WithScenarioStackTemplate(template)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldStack created = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                new WorldPosition(4, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                SubraceId = subrace.Id,
                AiPriority = 4,
                SourceTemplateId = templateId
            };

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackCreatedEvent(created, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(created.Id, created),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CreateStackNeutralOwnerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);
        WorldSubrace neutralSubrace = new(new WorldSubraceId("neutralHuman"), neutral.Id, WorldSubraceType.NeutralHuman, new WorldObjectVariant(6));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        ScenarioStackTemplateDefinition template = new(
            templateId,
            neutral.Id,
            neutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(leader),
            new WorldStackOrder.DefendStack(new WorldStackId("Missing")),
            Direction: null,
            AiPriority: 4);

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Camp"), new ScenarioTextId("Camp"), new WorldPosition(4, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventCreateStackEffect(templateId, location.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithPlayer(neutral)
            .WithSubrace(neutralSubrace)
            .WithScenarioLocation(location)
            .WithScenarioStackTemplate(template)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldStack created = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                neutral.Id,
                new WorldPosition(4, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                SubraceId = neutralSubrace.Id,
                AiPriority = 4,
                Order = WorldStackOrder.Roam.Instance,
                SourceTemplateId = templateId
            };

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackCreatedEvent(created, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(created.Id, created),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }
}
