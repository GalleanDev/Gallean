using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineScenarioEventEffectTests
{
    private static BattleState ExpectedBattleState(
        UnitDefinition attacker,
        WorldUnitId attackerId,
        string? attackerName,
        UnitDefinition defender,
        WorldUnitId defenderId,
        string? defenderName)
    {
        BattleUnit attackerUnit = new(
            Id: new BattleUnitId(0),
            WorldUnitId: attackerId,
            DefinitionId: attacker.Id,
            Side: BattleSide.Attacker,
            Hitpoints: attacker.MaxHitpoints,
            Experience: 0,
            ExperienceGained: 0,
            Level: attacker.Level,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: attackerName,
            IsTypeLocked: false);

        BattleUnit defenderUnit = new(
            Id: new BattleUnitId(1),
            WorldUnitId: defenderId,
            DefinitionId: defender.Id,
            Side: BattleSide.Defender,
            Hitpoints: defender.MaxHitpoints,
            Experience: 0,
            ExperienceGained: 0,
            Level: defender.Level,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: defenderName,
            IsTypeLocked: false);

        return new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(attackerUnit, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defenderUnit, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: default,
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);
    }

    private static UnitDefinition SummonedUnitDefinition(string id)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Summon,
            Leadership = 1,
            MovementPoints = 20
        };
    }

    private static WorldSpellDefinition SummonSpell(WorldSpellDefinitionId id, UnitDefinition unit, WorldResourceSet castingCost)
    {
        return TestHelpers.SimpleSpellDefinition(id, new WorldSummonStackSpellEffect(unit.Id)) with
        {
            CastingCost = castingCost
        };
    }

    private static WorldStack SummonedStack(
        WorldUnitId unitId,
        WorldStackId id,
        WorldPlayerId ownerId,
        WorldSubraceId subraceId,
        UnitDefinition unit,
        WorldPosition position)
    {
        return TestHelpers.SimpleStack(
                id,
                ownerId,
                position,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(unitId, unit), GridPosition.FrontCenter, unit.IsLarge)
                    .Build(),
                movementPoints: 20) with
            {
                SubraceId = subraceId
            };
    }

    [Test]
    public void WinEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventWinEffect(new WorldPlayerId(1))],
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
            new WorldScenarioEndedEvent(player.Id),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    IsEnded = true,
                    Winner = player.Id
                }
            }));
    }

    [Test]
    public void WinEffectLaterEventsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition winEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventWinEffect(new WorldPlayerId(1))],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        ScenarioEventDefinition laterEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The tale continues."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(winEvent)
            .WithScenarioEvent(laterEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(winEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEndedEvent(player.Id),
            new WorldScenarioEventTriggeredEvent(winEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(winEvent.Id, false),
                    IsEnded = true,
                    Winner = player.Id
                }
            }));
    }

    [Test]
    public void MoveStackEffectTest()
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

        ScenarioLocationDefinition destination = new(new ScenarioLocationId("Dest"), new ScenarioTextId("Destination"), new WorldPosition(4, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventMoveStackEffect(new ScenarioStackReference.Stack(stack.Id), destination.Id)],
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
            .WithScenarioLocation(destination)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 10),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(2, 0), new WorldPosition(3, 0), WorldStackDirection.SouthEast),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(3, 0), new WorldPosition(4, 0), WorldStackDirection.SouthEast),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(4, 0),
                    Direction = WorldStackDirection.SouthEast
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void MoveStackEffectCascadeTest()
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

        ScenarioLocationDefinition gate = new(new ScenarioLocationId("Gate"), new ScenarioTextId("Gate"), new WorldPosition(1, 0), 0);
        ScenarioLocationDefinition destination = new(new ScenarioLocationId("Dest"), new ScenarioTextId("Destination"), new WorldPosition(4, 0), 0);

        ScenarioEventDefinition moveEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(gate.Id)],
            [new ScenarioEventMoveStackEffect(new ScenarioStackReference.Stack(stack.Id), destination.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);
        ScenarioEventDefinition arrivalEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackToLocationCondition(new ScenarioStackReference.Stack(stack.Id), destination.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The escort has arrived."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioLocation(gate)
            .WithScenarioLocation(destination)
            .WithScenarioEvent(moveEvent)
            .WithScenarioEvent(arrivalEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, gate.Position),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 8, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 8, [], null),
                new WorldScenarioEventTriggeringEvent(moveEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldStackRelocatedEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast),
                new WorldStackRelocatedEvent(stack.Id, new WorldPosition(2, 0), new WorldPosition(3, 0), WorldStackDirection.SouthEast),
                new WorldStackRelocatedEvent(stack.Id, new WorldPosition(3, 0), new WorldPosition(4, 0), WorldStackDirection.SouthEast),
                new WorldScenarioEventTriggeredEvent(moveEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioEventTriggeringEvent(arrivalEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioPopupEvent(arrivalEvent.Id, new ScenarioTextId("The escort has arrived."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(4, 0), [player.Id]),
                new WorldScenarioEventTriggeredEvent(arrivalEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(4, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 8
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty
                        .Add(moveEvent.Id, false)
                        .Add(arrivalEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            arrivalEvent.Id,
                            new ScenarioTextId("The escort has arrived."),
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
    public void MoveStackEffectMultipleMovesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 20);

        ScenarioLocationDefinition first = new(new ScenarioLocationId("First"), new ScenarioTextId("First"), new WorldPosition(2, 0), 0);
        ScenarioLocationDefinition second = new(new ScenarioLocationId("Second"), new ScenarioTextId("Second"), new WorldPosition(2, 2), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventMoveStackEffect(new ScenarioStackReference.Stack(stack.Id), first.Id),
                new ScenarioEventMoveStackEffect(new ScenarioStackReference.Stack(stack.Id), second.Id)
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
            .WithScenarioLocation(first)
            .WithScenarioLocation(second)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(2, 2),
                    Direction = WorldStackDirection.SouthWest
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void MoveStackEffectMissingStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioLocationDefinition destination = new(new ScenarioLocationId("Dest"), new ScenarioTextId("Destination"), new WorldPosition(4, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventMoveStackEffect(new ScenarioStackReference.Stack(new WorldStackId("Missing")), destination.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioLocation(destination)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);
    }

    [Test]
    public void MoveStackEffectFirstTemplateInstanceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack raider = TestHelpers.SimpleStack(
                new WorldStackId("Raider1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 10) with
            {
                SourceTemplateId = templateId
            };

        ScenarioLocationDefinition destination = new(new ScenarioLocationId("Dest"), new ScenarioTextId("Destination"), new WorldPosition(4, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventMoveStackEffect(new ScenarioStackReference.Template(templateId), destination.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(raider)
            .WithScenarioLocation(destination)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(raider.Id, raider with
                {
                    Position = new WorldPosition(4, 0),
                    Direction = WorldStackDirection.SouthEast
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void MoveStackEffectUninstantiatedTemplateTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioLocationDefinition destination = new(new ScenarioLocationId("Dest"), new ScenarioTextId("Destination"), new WorldPosition(4, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventMoveStackEffect(new ScenarioStackReference.Template(new ScenarioStackTemplateId("Template1")), destination.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioLocation(destination)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);
    }

    [Test]
    public void MoveStackEffectTriggersNothingTest()
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

        ScenarioLocationDefinition destination = new(new ScenarioLocationId("Dest"), new ScenarioTextId("Destination"), new WorldPosition(4, 0), 0);
        ScenarioLocationDefinition crossing = new(new ScenarioLocationId("Cross"), new ScenarioTextId("Crossing"), new WorldPosition(2, 0), 0);

        ScenarioEventDefinition moveEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventMoveStackEffect(new ScenarioStackReference.Stack(stack.Id), destination.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        ScenarioEventDefinition crossingEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(crossing.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Spotted."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioLocation(destination)
            .WithScenarioLocation(crossing)
            .WithScenarioEvent(moveEvent)
            .WithScenarioEvent(crossingEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 10),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(moveEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(2, 0), new WorldPosition(3, 0), WorldStackDirection.SouthEast),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(3, 0), new WorldPosition(4, 0), WorldStackDirection.SouthEast),
            new WorldScenarioEventTriggeredEvent(moveEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(4, 0),
                    Direction = WorldStackDirection.SouthEast
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(moveEvent.Id, false)
                }
            }));
    }

    [Test]
    public void MoveStackEffectWithoutPathTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^.",
            ".^.",
            ".^."
        ]);

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

        ScenarioLocationDefinition destination = new(new ScenarioLocationId("Dest"), new ScenarioTextId("Destination"), new WorldPosition(2, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventMoveStackEffect(new ScenarioStackReference.Stack(stack.Id), destination.Id)],
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
            .WithScenarioLocation(destination)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 10),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(2, 0),
                    Direction = WorldStackDirection.SouthEast
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void MoveBesideEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

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

        WorldStack ambush = TestHelpers.SimpleStack(
            new WorldStackId("Ambush"),
            enemy.Id,
            new WorldPosition(4, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventMoveBesideEffect(new ScenarioStackReference.Stack(ambush.Id))],
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
            .WithStack(ambush)
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
                new WorldStackRelocatedEvent(ambush.Id, new WorldPosition(4, 2), new WorldPosition(3, 1), WorldStackDirection.North),
                new WorldStackRelocatedEvent(ambush.Id, new WorldPosition(3, 1), new WorldPosition(2, 0), WorldStackDirection.North),
                new WorldStackRelocatedEvent(ambush.Id, new WorldPosition(2, 0), new WorldPosition(1, 0), WorldStackDirection.West),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack.Id, stack with
                    {
                        Position = new WorldPosition(0, 1),
                        Direction = WorldStackDirection.SouthWest,
                        MovementPoints = 4
                    })
                    .SetItem(ambush.Id, ambush with
                    {
                        Position = new WorldPosition(1, 0),
                        Direction = WorldStackDirection.West
                    }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void MoveBesideEffectTriggersNothingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

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

        WorldStack ambush = TestHelpers.SimpleStack(
            new WorldStackId("Ambush"),
            enemy.Id,
            new WorldPosition(4, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);
        ScenarioLocationDefinition landing = new(new ScenarioLocationId("Zone2"), new ScenarioTextId("Landing"), new WorldPosition(0, 0), 0);

        ScenarioEventDefinition ambushEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventMoveBesideEffect(new ScenarioStackReference.Stack(ambush.Id))],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        ScenarioEventDefinition landingEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Undead],
            [WorldRace.Undead],
            [new ScenarioEventEnteringZoneCondition(landing.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Arrived."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(ambush)
            .WithScenarioLocation(zone)
            .WithScenarioLocation(landing)
            .WithScenarioEvent(ambushEvent)
            .WithScenarioEvent(landingEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null),
                new WorldScenarioEventTriggeringEvent(ambushEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldStackRelocatedEvent(ambush.Id, new WorldPosition(4, 2), new WorldPosition(3, 1), WorldStackDirection.North),
                new WorldStackRelocatedEvent(ambush.Id, new WorldPosition(3, 1), new WorldPosition(2, 0), WorldStackDirection.North),
                new WorldStackRelocatedEvent(ambush.Id, new WorldPosition(2, 0), new WorldPosition(1, 0), WorldStackDirection.West),
                new WorldScenarioEventTriggeredEvent(ambushEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack.Id, stack with
                    {
                        Position = new WorldPosition(0, 1),
                        Direction = WorldStackDirection.SouthWest,
                        MovementPoints = 4
                    })
                    .SetItem(ambush.Id, ambush with
                    {
                        Position = new WorldPosition(1, 0),
                        Direction = WorldStackDirection.West
                    }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(ambushEvent.Id, false)
                }
            }));
    }

    [Test]
    public void MoveBesideEffectTurnStartTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack ambush = TestHelpers.SimpleStack(
            new WorldStackId("Ambush"),
            player.Id,
            new WorldPosition(4, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventMoveBesideEffect(new ScenarioStackReference.Stack(ambush.Id))],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(ambush)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(ambush.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);
    }

    [Test]
    public void MoveBesideEffectAdjacentStackTest()
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

        WorldStack escort = TestHelpers.SimpleStack(
            new WorldStackId("Escort"),
            player.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Meeting"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventMoveBesideEffect(new ScenarioStackReference.Stack(escort.Id))],
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
            .WithStack(escort)
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
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(0, 1),
                    Direction = WorldStackDirection.SouthWest,
                    MovementPoints = 4
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void MoveBesideEffectNoFreeTileTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...~",
            "...~"
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition sailor = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("sailor", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                IsWaterOnly = true
            };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack fleet = TestHelpers.SimpleStack(
            new WorldStackId("Fleet"),
            enemy.Id,
            new WorldPosition(3, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("sailor"), sailor),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(1, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventMoveBesideEffect(new ScenarioStackReference.Stack(fleet.Id))],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(sailor)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(fleet)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(1, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 4
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void MoveBesideEffectNearestTileTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^.^",
            "^.^",
            "^.^",
            "^.^",
            "^.^"
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack ambush = TestHelpers.SimpleStack(
            new WorldStackId("Ambush"),
            enemy.Id,
            new WorldPosition(1, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Pass"), new WorldPosition(1, 2), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventMoveBesideEffect(new ScenarioStackReference.Stack(ambush.Id))],
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
            .WithStack(ambush)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 2)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(1, 1), new WorldPosition(1, 2), WorldStackDirection.SouthWest, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(1, 1), new WorldPosition(1, 2), WorldStackDirection.SouthWest, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldStackRelocatedEvent(ambush.Id, new WorldPosition(1, 4), new WorldPosition(1, 3), WorldStackDirection.NorthEast),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack.Id, stack with
                    {
                        Position = new WorldPosition(1, 2),
                        Direction = WorldStackDirection.SouthWest,
                        MovementPoints = 4
                    })
                    .SetItem(ambush.Id, ambush with
                    {
                        Position = new WorldPosition(1, 3),
                        Direction = WorldStackDirection.NorthEast
                    }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void MoveBesideEffectFirstTemplateInstanceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack first = TestHelpers.SimpleStack(
                new WorldStackId("First"),
                enemy.Id,
                new WorldPosition(4, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        WorldStack second = TestHelpers.SimpleStack(
                new WorldStackId("Second"),
                enemy.Id,
                new WorldPosition(4, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventMoveBesideEffect(new ScenarioStackReference.Template(templateId))],
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
            .WithStack(first)
            .WithStack(second)
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
                new WorldStackRelocatedEvent(first.Id, new WorldPosition(4, 0), new WorldPosition(3, 0), WorldStackDirection.NorthWest),
                new WorldStackRelocatedEvent(first.Id, new WorldPosition(3, 0), new WorldPosition(2, 0), WorldStackDirection.NorthWest),
                new WorldStackRelocatedEvent(first.Id, new WorldPosition(2, 0), new WorldPosition(1, 0), WorldStackDirection.West),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack.Id, stack with
                    {
                        Position = new WorldPosition(0, 1),
                        Direction = WorldStackDirection.SouthWest,
                        MovementPoints = 4
                    })
                    .SetItem(first.Id, first with
                    {
                        Position = new WorldPosition(1, 0),
                        Direction = WorldStackDirection.West
                    })
                    .SetItem(second.Id, second),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CreateStackEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        ScenarioStackTemplateDefinition template = new(
            templateId,
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(leader),
            WorldStackOrder.Normal.Instance,
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
            .WithScenarioLocation(location)
            .WithScenarioStackTemplate(template)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldStackId createdId = new("Stack0001");

        WorldStack created = TestHelpers.SimpleStack(
                createdId,
                player.Id,
                new WorldPosition(4, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
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
    public void CreateStackEffectTemplateFacingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        ScenarioStackTemplateDefinition template = new(
            templateId,
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(leader),
            WorldStackOrder.Stand.Instance,
            WorldStackDirection.NorthWest,
            AiPriority: 3);

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Camp"), new ScenarioTextId("Camp"), new WorldPosition(0, 0), 0);

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
            .WithScenarioLocation(location)
            .WithScenarioStackTemplate(template)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldStack created = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                Direction = WorldStackDirection.NorthWest,
                Order = WorldStackOrder.Stand.Instance,
                SourceTemplateId = templateId
            };

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
    public void CreateStackEffectOccupiedCenterTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack blocker = TestHelpers.SimpleStack(
            new WorldStackId("Blocker"),
            player.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("blocker"), leader),
            movementPoints: 6);

        ScenarioStackTemplateDefinition template = new(
            templateId,
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(leader),
            WorldStackOrder.Normal.Instance,
            Direction: null,
            AiPriority: 3);

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Camp"), new ScenarioTextId("Camp"), new WorldPosition(1, 0), 1);

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
            .WithStack(blocker)
            .WithScenarioLocation(location)
            .WithScenarioStackTemplate(template)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldStackId createdId = new("Stack0001");

        WorldStack created = TestHelpers.SimpleStack(
                createdId,
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(blocker.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackCreatedEvent(created, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(blocker.Id, blocker)
                    .SetItem(created.Id, created),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CreateStackEffectFullLocationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack blocker = TestHelpers.SimpleStack(
            new WorldStackId("Blocker"),
            player.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioStackTemplateDefinition template = new(
            templateId,
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(leader),
            WorldStackOrder.Normal.Instance,
            Direction: null,
            AiPriority: 3);

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Camp"), new ScenarioTextId("Camp"), new WorldPosition(1, 0), 0);

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
            .WithStack(blocker)
            .WithScenarioLocation(location)
            .WithScenarioStackTemplate(template)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(blocker.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);
    }

    [Test]
    public void CreateStackEffectLaterEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        ScenarioStackTemplateDefinition template = new(
            templateId,
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(leader),
            WorldStackOrder.Normal.Instance,
            Direction: null,
            AiPriority: 3);

        ScenarioLocationDefinition camp = new(new ScenarioLocationId("Camp"), new ScenarioTextId("Camp"), new WorldPosition(0, 0), 0);
        ScenarioLocationDefinition target = new(new ScenarioLocationId("Target"), new ScenarioTextId("Target"), new WorldPosition(2, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventCreateStackEffect(templateId, camp.Id),
                new ScenarioEventMoveStackEffect(new ScenarioStackReference.Template(templateId), target.Id)
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
            .WithScenarioLocation(camp)
            .WithScenarioLocation(target)
            .WithScenarioStackTemplate(template)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldStack created = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                new WorldPosition(2, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                Direction = WorldStackDirection.SouthEast,
                SourceTemplateId = templateId
            };

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
    public void RemoveStackEffectTest()
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
            [new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Stack(stack.Id), FirstOnly: true)],
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
            new WorldStackRemovedEvent(stack.Id),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(stack.Id, DestroyerPlayerId: null, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void RemoveStackEffectMissingStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Stack(new WorldStackId("Missing")), FirstOnly: true)],
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
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);
    }

    [Test]
    public void RemoveStackEffectFirstTemplateInstanceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack first = TestHelpers.SimpleStack(
                new WorldStackId("Raider1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        WorldStack second = first with
        {
            Id = new WorldStackId("Raider2"),
            Position = new WorldPosition(2, 0),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("secondLeader"), leader)
        };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Template(templateId), FirstOnly: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(first)
            .WithStack(second)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.Remove(first.Id).SetItem(second.Id, second),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(first.Id, DestroyerPlayerId: null, templateId)]
                }
            }));
    }

    [Test]
    public void RemoveStackEffectAllTemplateInstancesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack first = TestHelpers.SimpleStack(
                new WorldStackId("Raider1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        WorldStack second = first with
        {
            Id = new WorldStackId("Raider2"),
            Position = new WorldPosition(2, 0),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("secondLeader"), leader)
        };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Template(templateId), FirstOnly: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(first)
            .WithStack(second)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(first.Id, 6),
            new WorldStackMovementRestoredEvent(second.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRemovedEvent(first.Id),
            new WorldStackRemovedEvent(second.Id),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks =
                    [
                        new ScenarioDestroyedStack(first.Id, DestroyerPlayerId: null, templateId),
                        new ScenarioDestroyedStack(second.Id, DestroyerPlayerId: null, templateId)
                    ]
                }
            }));
    }

    [Test]
    public void UnfogEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "?????",
                "?????",
                "?????"
            ])
        };

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Watchtower"), new WorldPosition(1, 1), 2);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventUnfogEffect(zone.Id, Reveal: true, Radius: null, [player.Id])],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldFogRevealedEvent(player.Id, [
                new WorldPosition(0, 0),
                new WorldPosition(1, 0),
                new WorldPosition(2, 0),
                new WorldPosition(3, 0),
                new WorldPosition(0, 1),
                new WorldPosition(1, 1),
                new WorldPosition(2, 1),
                new WorldPosition(3, 1),
                new WorldPosition(0, 2),
                new WorldPosition(1, 2),
                new WorldPosition(2, 2),
                new WorldPosition(3, 2)
            ]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player.Id].FogOfWar), Is.EqualTo([
            "....?",
            "....?",
            "....?"
        ]));
    }

    [Test]
    public void UnfogEffectSmallerRadiusTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "?????",
                "?????",
                "?????",
                "?????",
                "?????"
            ])
        };

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Watchtower"), new WorldPosition(2, 2), 2);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventUnfogEffect(zone.Id, Reveal: true, Radius: 0, [player.Id])],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldFogRevealedEvent(player.Id, [new WorldPosition(2, 2)]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player.Id].FogOfWar), Is.EqualTo([
            "?????",
            "?????",
            "??.??",
            "?????",
            "?????"
        ]));
    }

    [Test]
    public void UnfogEffectLargerRadiusTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "???????",
                "???????",
                "???????",
                "???????",
                "???????"
            ])
        };

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Watchtower"), new WorldPosition(2, 2), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventUnfogEffect(zone.Id, Reveal: true, Radius: 2, [player.Id])],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldFogRevealedEvent(player.Id, [
                new WorldPosition(0, 0),
                new WorldPosition(1, 0),
                new WorldPosition(2, 0),
                new WorldPosition(3, 0),
                new WorldPosition(4, 0),
                new WorldPosition(0, 1),
                new WorldPosition(1, 1),
                new WorldPosition(2, 1),
                new WorldPosition(3, 1),
                new WorldPosition(4, 1),
                new WorldPosition(0, 2),
                new WorldPosition(1, 2),
                new WorldPosition(2, 2),
                new WorldPosition(3, 2),
                new WorldPosition(4, 2),
                new WorldPosition(0, 3),
                new WorldPosition(1, 3),
                new WorldPosition(2, 3),
                new WorldPosition(3, 3),
                new WorldPosition(4, 3),
                new WorldPosition(0, 4),
                new WorldPosition(1, 4),
                new WorldPosition(2, 4),
                new WorldPosition(3, 4),
                new WorldPosition(4, 4)
            ]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player.Id].FogOfWar), Is.EqualTo([
            ".....??",
            ".....??",
            ".....??",
            ".....??",
            ".....??"
        ]));
    }

    [Test]
    public void UnfogEffectAudienceOnlyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer triggerer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "???",
                "???",
                "???"
            ])
        };

        WorldPlayer ally = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "???",
                "???",
                "???"
            ])
        };

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Watchtower"), new WorldPosition(1, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventUnfogEffect(zone.Id, Reveal: true, Radius: null, [ally.Id])],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(triggerer.LordId, triggerer.Race, LordCategory.Warrior))
            .WithPlayer(triggerer)
            .WithDefinition(TestHelpers.SimpleLord(ally.LordId, ally.Race, LordCategory.Warrior))
            .WithPlayer(ally)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(triggerer.Id),
            new WorldTurnStartedEvent(triggerer.Id),
            new WorldTurnResourcesGeneratedEvent(triggerer.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, triggerer.Id, new ScenarioEventTurnStartedTrigger(triggerer.Id)),
            new WorldFogRevealedEvent(ally.Id, [new WorldPosition(1, 1)]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, triggerer.Id, new ScenarioEventTurnStartedTrigger(triggerer.Id))
        ]);

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[triggerer.Id].FogOfWar), Is.EqualTo([
            "???",
            "???",
            "???"
        ]));
        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[ally.Id].FogOfWar), Is.EqualTo([
            "???",
            "?.?",
            "???"
        ]));
    }

    [Test]
    public void UnfogEffectRevealedZoneTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Watchtower"), new WorldPosition(1, 1), 1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventUnfogEffect(zone.Id, Reveal: true, Radius: null, [player.Id])],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);
    }

    [Test]
    public void GiveItemToCapitalEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);
        ItemDefinition itemDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("Item"), ItemCategory.Valuable);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventGiveItemEffect(itemDefinition.Id, ScenarioEventGiveItemTarget.TriggererCapital)],
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

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldItemsReceivedEvent(
                player.Id,
                new WorldUnitGroup.Fortification(capital.Id),
                [new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(
                    capital.Id,
                    capital with { Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)]) }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void GiveItemToTriggererEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

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
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            player.Id,
            new WorldPosition(9, 9),
            UnitGrid<WorldUnit>.Empty);
        ItemDefinition itemDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("Item"), ItemCategory.Valuable);
        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Shrine"), new WorldPosition(0, 1), 0);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventGiveItemEffect(itemDefinition.Id, ScenarioEventGiveItemTarget.Triggerer)],
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
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(0, 1)));

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(0, 1),
                    Direction = WorldStackDirection.SouthWest,
                    MovementPoints = 4,
                    Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)])
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void GiveItemToTriggererOnKillTest()
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

        WorldStack guardian = TestHelpers.SimpleStack(
            new WorldStackId("Guardian"),
            enemy.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldStack rearguard = TestHelpers.SimpleStack(
            new WorldStackId("Rearguard"),
            enemy.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            movementPoints: 6);

        ItemDefinition itemDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("Item"), ItemCategory.Valuable);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(guardian.Id))],
            [new ScenarioEventGiveItemEffect(itemDefinition.Id, ScenarioEventGiveItemTarget.Triggerer)],
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
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(guardian)
            .WithStack(rearguard)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack.Id, guardian.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(
                stack.Id,
                guardian.Id,
                BattleSide.Attacker,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                UnitGrid<WorldUnit>.Empty),
            new WorldStackDestroyedEvent(guardian.Id, new WorldUnitGroup.Stack(stack.Id), stack.OwnerId),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id))),
            new WorldItemsReceivedEvent(
                player.Id,
                new WorldUnitGroup.Stack(stack.Id),
                [new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id)))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)]),
                    BattlesWon = 1
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(guardian.Id, stack.OwnerId, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void GiveItemToTriggererWithoutStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);
        ItemDefinition itemDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("Item"), ItemCategory.Valuable);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventGiveItemEffect(itemDefinition.Id, ScenarioEventGiveItemTarget.Triggerer)],
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
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void RemoveMountainEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1, '^');
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldMountain mountain = new(
            new WorldMountainId(1),
            new WorldFootprint(new WorldPosition(1, 0), 3, 1),
            WorldRace.Neutral,
            new WorldObjectVariant(2));
        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Pass"),
            new ScenarioTextId("Pass"),
            new WorldPosition(2, 0),
            1);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventRemoveMountainEffect(location.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithMountain(mountain)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldMountainsRemovedEvent(location.Id, [mountain.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Map = TestHelpers.BuildMap(["^...^"]),
                Mountains = ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void RemoveMountainEffectWithoutMountainsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Pass"),
            new ScenarioTextId("Pass"),
            new WorldPosition(2, 0),
            1);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventRemoveMountainEffect(location.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);
    }

    [Test]
    public void RemoveLandmarkEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldLandmark landmark = new(
            new WorldLandmarkId("Landmark"),
            new WorldLandmarkDefinitionId("Old"),
            new WorldFootprint(new WorldPosition(0, 0), 1, 1));
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventRemoveLandmarkEffect(landmark.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithLandmark(landmark)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Landmarks = ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeObjectiveEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventChangeObjectiveEffect(new ScenarioTextId("Capture the city."))],
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

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    ObjectiveTextId = new ScenarioTextId("Capture the city.")
                }
            }));
    }

    [Test]
    public void ChangeLandmarkEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldLandmark landmark = new(
            new WorldLandmarkId("Landmark"),
            new WorldLandmarkDefinitionId("Old"),
            new WorldFootprint(new WorldPosition(0, 0), 1, 1));
        WorldLandmarkDefinition replacement = new(
            new WorldLandmarkDefinitionId("New"),
            new GlobalTextId("Name"),
            new GlobalTextId("Description"),
            WorldLandmarkType.Structure,
            Mountain: false,
            Width: 2,
            Height: 3);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventChangeLandmarkEffect(landmark.Id, replacement.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithLandmark(landmark)
            .WithDefinition(replacement)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Landmarks = engine.State.Landmarks.SetItem(
                    landmark.Id,
                    landmark with
                    {
                        DefinitionId = replacement.Id,
                        Footprint = new WorldFootprint(new WorldPosition(0, 0), 2, 3)
                    }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeTerrainEffectTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".....",
            ".....",
            "..^..",
            ".....",
            "....."
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Grove"),
            new ScenarioTextId("Grove"),
            new WorldPosition(2, 2),
            0);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventChangeTerrainEffect(location.Id, WorldMapTerrain.Elf, 3)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Map = TestHelpers.BuildMap([
                    ".....",
                    ".EEE.",
                    ".E^E.",
                    ".EEE.",
                    "....."
                ]),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOwnerEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer newOwner = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldSubrace newSubrace = new(
            new WorldSubraceId("undead"),
            newOwner.Id,
            WorldSubraceType.Undead,
            new WorldObjectVariant(5));
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
            [new ScenarioEventChangeOwnerEffect(new ScenarioStackReference.Stack(stack.Id), newSubrace.Id, FirstOnly: true, PlayAnimation: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(newOwner.LordId, newOwner.Race, LordCategory.Warrior))
            .WithPlayer(newOwner)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithSubrace(newSubrace)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOwnerChangedEvent(stack.Id, newOwner.Id, newSubrace.Id, PlayAnimation: true),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    OwnerId = newOwner.Id,
                    SubraceId = newSubrace.Id
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOwnerEffectWithoutAnimationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer newOwner = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldSubrace newSubrace = new(
            new WorldSubraceId("undead"),
            newOwner.Id,
            WorldSubraceType.Undead,
            new WorldObjectVariant(5));
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
            [new ScenarioEventChangeOwnerEffect(new ScenarioStackReference.Stack(stack.Id), newSubrace.Id, FirstOnly: true, PlayAnimation: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(newOwner.LordId, newOwner.Race, LordCategory.Warrior))
            .WithPlayer(newOwner)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithSubrace(newSubrace)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOwnerChangedEvent(stack.Id, newOwner.Id, newSubrace.Id, PlayAnimation: false),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    OwnerId = newOwner.Id,
                    SubraceId = newSubrace.Id
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOwnerEffectCurrentOwnerTest()
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
                new ScenarioEventChangeOwnerEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    TestHelpers.NeutralSubrace.Id,
                    FirstOnly: true,
                    PlayAnimation: true)
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
            new WorldStackOwnerChangedEvent(stack.Id, player.Id, TestHelpers.NeutralSubrace.Id, PlayAnimation: true),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

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
    public void ChangeOwnerEffectAllTemplateInstancesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer newOwner = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldSubrace newSubrace = new(
            new WorldSubraceId("undead"),
            newOwner.Id,
            WorldSubraceType.Undead,
            new WorldObjectVariant(5));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack first = TestHelpers.SimpleStack(
                new WorldStackId("Raider1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        WorldStack second = first with
        {
            Id = new WorldStackId("Raider2"),
            Position = new WorldPosition(2, 0),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("secondLeader"), leader)
        };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOwnerEffect(
                    new ScenarioStackReference.Template(templateId),
                    newSubrace.Id,
                    FirstOnly: false,
                    PlayAnimation: true)
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
            .WithDefinition(TestHelpers.SimpleLord(newOwner.LordId, newOwner.Race, LordCategory.Warrior))
            .WithPlayer(newOwner)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithSubrace(newSubrace)
            .WithStack(first)
            .WithStack(second)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(first.Id, 6),
            new WorldStackMovementRestoredEvent(second.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOwnerChangedEvent(first.Id, newOwner.Id, newSubrace.Id, PlayAnimation: true),
            new WorldStackOwnerChangedEvent(second.Id, newOwner.Id, newSubrace.Id, PlayAnimation: true),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(first.Id, first with { OwnerId = newOwner.Id, SubraceId = newSubrace.Id })
                    .SetItem(second.Id, second with { OwnerId = newOwner.Id, SubraceId = newSubrace.Id }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOwnerEffectFirstTemplateInstanceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer newOwner = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldSubrace newSubrace = new(
            new WorldSubraceId("undead"),
            newOwner.Id,
            WorldSubraceType.Undead,
            new WorldObjectVariant(5));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack first = TestHelpers.SimpleStack(
                new WorldStackId("Raider1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        WorldStack second = first with
        {
            Id = new WorldStackId("Raider2"),
            Position = new WorldPosition(2, 0),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("secondLeader"), leader)
        };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOwnerEffect(
                    new ScenarioStackReference.Template(templateId),
                    newSubrace.Id,
                    FirstOnly: true,
                    PlayAnimation: true)
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
            .WithDefinition(TestHelpers.SimpleLord(newOwner.LordId, newOwner.Race, LordCategory.Warrior))
            .WithPlayer(newOwner)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithSubrace(newSubrace)
            .WithStack(first)
            .WithStack(second)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(first.Id, first with { OwnerId = newOwner.Id, SubraceId = newSubrace.Id })
                    .SetItem(second.Id, second),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOwnerEffectLeavesCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer newOwner = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldSubrace newSubrace = new(
            new WorldSubraceId("undead"),
            newOwner.Id,
            WorldSubraceType.Undead,
            new WorldObjectVariant(5));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            city.Entrance,
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventChangeOwnerEffect(new ScenarioStackReference.Stack(stack.Id), newSubrace.Id, FirstOnly: true, PlayAnimation: true)],
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
            .WithDefinition(TestHelpers.SimpleLord(newOwner.LordId, newOwner.Race, LordCategory.Warrior))
            .WithPlayer(newOwner)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithSubrace(newSubrace)
            .WithCity(city)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(3, 3), new WorldPosition(4, 4), WorldStackDirection.South),
            new WorldFortificationExitedEvent(stack.Id, city.Id),
            new WorldStackOwnerChangedEvent(stack.Id, newOwner.Id, newSubrace.Id, PlayAnimation: true),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    OwnerId = newOwner.Id,
                    SubraceId = newSubrace.Id,
                    Position = new WorldPosition(4, 4)
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOwnerEffectStackWithoutExitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".....",
            ".....",
            "....^",
            "....^",
            "..^^^"
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer newOwner = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldSubrace newSubrace = new(
            new WorldSubraceId("undead"),
            newOwner.Id,
            WorldSubraceType.Undead,
            new WorldObjectVariant(5));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            city.Entrance,
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventChangeOwnerEffect(new ScenarioStackReference.Stack(stack.Id), newSubrace.Id, FirstOnly: true, PlayAnimation: true)],
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
            .WithDefinition(TestHelpers.SimpleLord(newOwner.LordId, newOwner.Race, LordCategory.Warrior))
            .WithPlayer(newOwner)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithSubrace(newSubrace)
            .WithCity(city)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOwnerChangedEvent(stack.Id, newOwner.Id, newSubrace.Id, PlayAnimation: true),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    OwnerId = newOwner.Id,
                    SubraceId = newSubrace.Id
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOwnerEffectExitOrderTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".....",
            ".....",
            ".....",
            ".....",
            "....^"
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer newOwner = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldSubrace newSubrace = new(
            new WorldSubraceId("undead"),
            newOwner.Id,
            WorldSubraceType.Undead,
            new WorldObjectVariant(5));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            city.Entrance,
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventChangeOwnerEffect(new ScenarioStackReference.Stack(stack.Id), newSubrace.Id, FirstOnly: true, PlayAnimation: true)],
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
            .WithDefinition(TestHelpers.SimpleLord(newOwner.LordId, newOwner.Race, LordCategory.Warrior))
            .WithPlayer(newOwner)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithSubrace(newSubrace)
            .WithCity(city)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(3, 3), new WorldPosition(3, 4), WorldStackDirection.SouthWest),
            new WorldFortificationExitedEvent(stack.Id, city.Id),
            new WorldStackOwnerChangedEvent(stack.Id, newOwner.Id, newSubrace.Id, PlayAnimation: true),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    OwnerId = newOwner.Id,
                    SubraceId = newSubrace.Id,
                    Position = new WorldPosition(3, 4),
                    Direction = WorldStackDirection.SouthWest
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOwnerEffectLeavesNewOwnerCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer newOwner = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldSubrace newSubrace = new(
            new WorldSubraceId("undead"),
            newOwner.Id,
            WorldSubraceType.Undead,
            new WorldObjectVariant(5));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            newOwner.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            city.Entrance,
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventChangeOwnerEffect(new ScenarioStackReference.Stack(stack.Id), newSubrace.Id, FirstOnly: true, PlayAnimation: true)],
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
            .WithDefinition(TestHelpers.SimpleLord(newOwner.LordId, newOwner.Race, LordCategory.Warrior))
            .WithPlayer(newOwner)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithSubrace(newSubrace)
            .WithCity(city)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(3, 3), new WorldPosition(4, 4), WorldStackDirection.South),
            new WorldFortificationExitedEvent(stack.Id, city.Id),
            new WorldStackOwnerChangedEvent(stack.Id, newOwner.Id, newSubrace.Id, PlayAnimation: true),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    OwnerId = newOwner.Id,
                    SubraceId = newSubrace.Id,
                    Position = new WorldPosition(4, 4)
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOwnerEffectCurrentOwnerLeavesCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            city.Entrance,
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOwnerEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    TestHelpers.NeutralSubrace.Id,
                    FirstOnly: true,
                    PlayAnimation: true)
            ],
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
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithCity(city)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRelocatedEvent(stack.Id, new WorldPosition(3, 3), new WorldPosition(4, 4), WorldStackDirection.South),
            new WorldFortificationExitedEvent(stack.Id, city.Id),
            new WorldStackOwnerChangedEvent(stack.Id, player.Id, TestHelpers.NeutralSubrace.Id, PlayAnimation: true),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(4, 4)
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOrderEffectTest()
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
                    WorldStackOrder.Berserk.Instance,
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
            new WorldStackOrderChangedEvent(stack.Id, WorldStackOrder.Berserk.Instance),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Order = WorldStackOrder.Berserk.Instance
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOrderEffectTargetedOrderTest()
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

        WorldStack enemy = stack with
        {
            Id = new WorldStackId("Stack2"),
            Position = new WorldPosition(2, 0),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("enemyLeader"), leader)
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
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithStack(enemy)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldStackMovementRestoredEvent(enemy.Id, 6),
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
    public void ChangeOrderEffectMissingStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(new WorldStackId("Missing")),
                    WorldStackOrder.Berserk.Instance,
                    FirstOnly: true)
            ],
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
    public void ChangeOrderEffectAllTemplateInstancesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack first = TestHelpers.SimpleStack(
                new WorldStackId("Raider1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        WorldStack second = first with
        {
            Id = new WorldStackId("Raider2"),
            Position = new WorldPosition(2, 0),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("secondLeader"), leader)
        };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Template(templateId),
                    WorldStackOrder.Berserk.Instance,
                    FirstOnly: false)
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
            .WithStack(first)
            .WithStack(second)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(first.Id, 6),
            new WorldStackMovementRestoredEvent(second.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOrderChangedEvent(first.Id, WorldStackOrder.Berserk.Instance),
            new WorldStackOrderChangedEvent(second.Id, WorldStackOrder.Berserk.Instance),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(first.Id, first with { Order = WorldStackOrder.Berserk.Instance })
                    .SetItem(second.Id, second with { Order = WorldStackOrder.Berserk.Instance }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOrderEffectFirstTemplateInstanceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack first = TestHelpers.SimpleStack(
                new WorldStackId("Raider1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        WorldStack second = first with
        {
            Id = new WorldStackId("Raider2"),
            Position = new WorldPosition(2, 0),
            Units = TestHelpers.SingleUnitGrid(new WorldUnitId("secondLeader"), leader)
        };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Template(templateId),
                    WorldStackOrder.Berserk.Instance,
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
            .WithStack(first)
            .WithStack(second)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(first.Id, 6),
            new WorldStackMovementRestoredEvent(second.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOrderChangedEvent(first.Id, WorldStackOrder.Berserk.Instance),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(first.Id, first with { Order = WorldStackOrder.Berserk.Instance }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOrderEffectAfterCreateStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        ScenarioStackTemplateDefinition template = new(
            templateId,
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(leader),
            WorldStackOrder.Normal.Instance,
            Direction: null,
            AiPriority: 3);

        ScenarioLocationDefinition camp = new(new ScenarioLocationId("Camp"), new ScenarioTextId("Camp"), new WorldPosition(0, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventCreateStackEffect(templateId, camp.Id),
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Template(templateId),
                    WorldStackOrder.Berserk.Instance,
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
            .WithScenarioLocation(camp)
            .WithScenarioStackTemplate(template)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldStack created = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                Order = WorldStackOrder.Berserk.Instance,
                SourceTemplateId = templateId
            };

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
    public void ChangeOrderEffectForgetsAiOrderTest()
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

        WorldAiMemory memory = new(
            new WorldAiTurnMemory(1, player.Id),
            ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty.Add(
                stack.Id,
                new WorldAiStackMemory(
                    WorldStackOrder.Explore.Instance,
                    new WorldAiTarget.Location(new WorldPosition(2, 0)))));

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    WorldStackOrder.Stand.Instance,
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
            .WithAiMemory(player.Id, memory)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Order = WorldStackOrder.Stand.Instance
                }),
                AiMemories = engine.State.AiMemories.SetItem(
                    player.Id,
                    memory with { Stacks = ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void BattleEffectTriggererTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

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

        WorldStack ambush = TestHelpers.SimpleStack(
            new WorldStackId("Ambush"),
            enemy.Id,
            new WorldPosition(4, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventBattleEffect(new ScenarioStackReference.Stack(ambush.Id))],
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
            .WithStack(ambush)
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
                new WorldStackRelocatedEvent(ambush.Id, new WorldPosition(4, 2), new WorldPosition(3, 1), WorldStackDirection.North),
                new WorldStackRelocatedEvent(ambush.Id, new WorldPosition(3, 1), new WorldPosition(2, 0), WorldStackDirection.North),
                new WorldStackRelocatedEvent(ambush.Id, new WorldPosition(2, 0), new WorldPosition(1, 0), WorldStackDirection.West),
                new WorldStackBattleStartedEvent(
                    ambush.Id,
                    stack.Id,
                    new WorldPosition(0, 1),
                    ExpectedBattleState(leader, new WorldUnitId("leader2"), attackerName: null, leader, new WorldUnitId("leader"), defenderName: null), IsDuel: false),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack.Id, stack with
                    {
                        Position = new WorldPosition(0, 1),
                        Direction = WorldStackDirection.SouthWest,
                        MovementPoints = 4
                    })
                    .SetItem(ambush.Id, ambush with
                    {
                        Position = new WorldPosition(1, 0),
                        Direction = WorldStackDirection.West
                    }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void BattleEffectAdjacentStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition raider = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("raider", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack ambush = TestHelpers.SimpleStack(
            new WorldStackId("Ambush"),
            enemy.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("raider"), raider),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventBattleEffect(new ScenarioStackReference.Stack(ambush.Id))],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(raider)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(ambush)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(
                    stack.Id,
                    new WorldPosition(0, 0),
                    new WorldPosition(0, 1),
                    WorldStackDirection.SouthWest,
                    4,
                    [], ambush.Position),
                new WorldStackMovedEvent(
                    stack.Id,
                    new WorldPosition(0, 0),
                    new WorldPosition(0, 1),
                    WorldStackDirection.SouthWest,
                    4,
                    [], ambush.Position),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldStackBattleStartedEvent(
                    ambush.Id,
                    stack.Id,
                    new WorldPosition(0, 1),
                    ExpectedBattleState(raider, new WorldUnitId("raider"), attackerName: null, leader, new WorldUnitId("leader"), defenderName: null), IsDuel: false),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(0, 1),
                    Direction = WorldStackDirection.SouthWest,
                    MovementPoints = 4
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void BattleEffectTurnStartTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack ambush = TestHelpers.SimpleStack(
            new WorldStackId("Ambush"),
            player.Id,
            new WorldPosition(4, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventBattleEffect(new ScenarioStackReference.Stack(ambush.Id))],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(ambush)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(ambush.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);
    }

    [Test]
    public void BattleEffectMissingStackTest()
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

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(1, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventBattleEffect(new ScenarioStackReference.Stack(new WorldStackId("Missing")))],
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
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);
    }

    [Test]
    public void BattleEffectNoFreeTileTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...~",
            "...~"
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition sailor = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("sailor", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                IsWaterOnly = true
            };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack fleet = TestHelpers.SimpleStack(
            new WorldStackId("Fleet"),
            enemy.Id,
            new WorldPosition(3, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("sailor"), sailor),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(1, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventBattleEffect(new ScenarioStackReference.Stack(fleet.Id))],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(sailor)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(fleet)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(1, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 4
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void BattleEffectCreatedTemplateInstanceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition raider = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("raider", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioStackTemplateDefinition template = new(
            templateId,
            enemy.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(raider),
            WorldStackOrder.Normal.Instance,
            Direction: null,
            AiPriority: 4);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(1, 0), 0);
        ScenarioLocationDefinition camp = new(new ScenarioLocationId("Camp"), new ScenarioTextId("Camp"), new WorldPosition(4, 0), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [
                new ScenarioEventCreateStackEffect(templateId, camp.Id),
                new ScenarioEventBattleEffect(new ScenarioStackReference.Template(templateId))
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(raider)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithScenarioLocation(zone)
            .WithScenarioLocation(camp)
            .WithScenarioStackTemplate(template)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldStackId createdId = new("Stack0001");

        WorldStack spawned = TestHelpers.SimpleStack(
                createdId,
                enemy.Id,
                new WorldPosition(4, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), raider) with { Name = "Konrad" }, GridPosition.FrontBottom, raider.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                AiPriority = 4,
                SourceTemplateId = templateId
            };

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldStackCreatedEvent(spawned, WorldResourceSet.Empty),
                new WorldStackRelocatedEvent(createdId, new WorldPosition(4, 0), new WorldPosition(3, 0), WorldStackDirection.NorthWest),
                new WorldStackRelocatedEvent(createdId, new WorldPosition(3, 0), new WorldPosition(2, 0), WorldStackDirection.NorthWest),
                new WorldStackBattleStartedEvent(
                    createdId,
                    stack.Id,
                    new WorldPosition(1, 0),
                    ExpectedBattleState(raider, new WorldUnitId("Unit0001"), attackerName: "Konrad", leader, new WorldUnitId("leader"), defenderName: null), IsDuel: false),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        WorldStack created = TestHelpers.SimpleStack(
                createdId,
                enemy.Id,
                new WorldPosition(2, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), raider) with { Name = "Konrad" }, GridPosition.FrontBottom, raider.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                Direction = WorldStackDirection.NorthWest,
                AiPriority = 4,
                SourceTemplateId = templateId
            };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack.Id, stack with
                    {
                        Position = new WorldPosition(1, 0),
                        Direction = WorldStackDirection.SouthEast,
                        MovementPoints = 4
                    })
                    .SetItem(createdId, created),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void BattleEffectEventsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition raider = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("raider", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack ambush = TestHelpers.SimpleStack(
            new WorldStackId("Ambush"),
            enemy.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("raider"), raider),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition battleEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [
                new ScenarioEventEnableEventEffect(new ScenarioEventId("Event2"), Enabled: true),
                new ScenarioEventBattleEffect(new ScenarioStackReference.Stack(ambush.Id))
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        ScenarioEventDefinition retreatEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Stack(ambush.Id), MustExist: true)],
            [new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Stack(ambush.Id), FirstOnly: true)],
            Enabled: false,
            OccurOnce: true,
            Chance: 100,
            Order: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(raider)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(ambush)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(battleEvent)
            .WithScenarioEvent(retreatEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(
                    stack.Id,
                    new WorldPosition(0, 0),
                    new WorldPosition(0, 1),
                    WorldStackDirection.SouthWest,
                    4,
                    [], ambush.Position),
                new WorldStackMovedEvent(
                    stack.Id,
                    new WorldPosition(0, 0),
                    new WorldPosition(0, 1),
                    WorldStackDirection.SouthWest,
                    4,
                    [], ambush.Position),
                new WorldScenarioEventTriggeringEvent(battleEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldStackBattleStartedEvent(
                    ambush.Id,
                    stack.Id,
                    new WorldPosition(0, 1),
                    ExpectedBattleState(raider, new WorldUnitId("raider"), attackerName: null, leader, new WorldUnitId("leader"), defenderName: null), IsDuel: false),
                new WorldScenarioEventTriggeredEvent(battleEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("leader"), leader.Id, BattleSide.Defender, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        engine.CheckCommand(new WorldResolveStackBattleCommand(ambush.Id, stack.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(
                ambush.Id,
                stack.Id,
                BattleSide.Defender,
                UnitGrid<WorldUnit>.Empty,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader)),
            new WorldStackDestroyedEvent(ambush.Id, new WorldUnitGroup.Stack(stack.Id), stack.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(stack.Id, stack with
                {
                    Position = new WorldPosition(0, 1),
                    Direction = WorldStackDirection.SouthWest,
                    MovementPoints = 4,
                    BattlesWon = 1
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty
                        .Add(battleEvent.Id, false)
                        .Add(retreatEvent.Id, true),
                    DestroyedStacks = [new ScenarioDestroyedStack(ambush.Id, stack.OwnerId, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void CastSpellEffectTriggererTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 100, 0, 0)
        };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldSpellDefinition spell = TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId("restore"), new WorldRestoreMovementSpellEffect(50)) with
        {
            CastingCost = new WorldResourceSet(0, 0, 0, 40, 0, 0)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Zone"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventCastSpellEffect(spell.Id, CasterId: null)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
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
                new WorldSpellCastEvent(
                    CasterId: null,
                    spell.Id,
                    new WorldSpellTarget(new WorldPosition(0, 1), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Scenario),
                new WorldStackMovementRestoredEvent(stack.Id, 6),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(100, 0, 0, 125, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(0, 1),
                    Direction = WorldStackDirection.SouthWest,
                    MovementPoints = 6
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CastSpellEffectWithoutTriggererTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition summon = SummonedUnitDefinition("summon");
        WorldSpellDefinition spell = SummonSpell(new WorldSpellDefinitionId("summon"), summon, WorldResourceSet.Empty);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventCastSpellEffect(spell.Id, CasterId: null)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
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
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CastSpellMapEffectCastingPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 100, 0, 0)
        };

        WorldSubrace enemySubrace = new(
            new WorldSubraceId("undead"),
            enemy.Id,
            WorldSubraceType.Undead,
            BannerVariant: new WorldObjectVariant(1));

        UnitDefinition summon = SummonedUnitDefinition("summon");
        WorldSpellDefinition spell = SummonSpell(
            new WorldSpellDefinitionId("summon"),
            summon,
            new WorldResourceSet(0, 0, 0, 40, 0, 0));

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Grave"), new WorldPosition(2, 1), 1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventCastSpellMapEffect(spell.Id, location.Id, enemy.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithSubrace(enemySubrace)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldStack summoned = SummonedStack(
            new WorldUnitId("Unit0001"),
            new WorldStackId("Stack0001"),
            enemy.Id,
            enemySubrace.Id,
            summon,
            new WorldPosition(2, 1));

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldSpellCastEvent(
                enemy.Id,
                spell.Id,
                new WorldSpellTarget(new WorldPosition(2, 1), Radius: 1),
                WorldResourceSet.Empty,
                WorldSpellCastSource.Scenario),
            new WorldStackCreatedEvent(summoned, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(enemy.Id, enemy),
                Stacks = engine.State.Stacks.SetItem(summoned.Id, summoned),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CastSpellMapEffectRacePlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer undead = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldSubrace undeadSubrace = new(
            new WorldSubraceId("undead"),
            undead.Id,
            WorldSubraceType.Undead,
            BannerVariant: new WorldObjectVariant(1));

        UnitDefinition summon = SummonedUnitDefinition("summon") with
        {
            Race = WorldRace.Undead
        };

        WorldSpellDefinition spell = SummonSpell(new WorldSpellDefinitionId("summon"), summon, WorldResourceSet.Empty);

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Grave"), new WorldPosition(2, 1), 1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventCastSpellMapEffect(spell.Id, location.Id, CasterId: null)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithDefinition(TestHelpers.SimpleLord(undead.LordId, undead.Race, LordCategory.Warrior))
            .WithPlayer(undead)
            .WithSubrace(undeadSubrace)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldStack summoned = SummonedStack(
            new WorldUnitId("Unit0001"),
            new WorldStackId("Stack0001"),
            undead.Id,
            undeadSubrace.Id,
            summon,
            new WorldPosition(2, 1));

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldSpellCastEvent(
                CasterId: null,
                spell.Id,
                new WorldSpellTarget(new WorldPosition(2, 1), Radius: 1),
                WorldResourceSet.Empty,
                WorldSpellCastSource.Scenario),
            new WorldStackCreatedEvent(summoned, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(summoned.Id, summoned),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CastSpellMapEffectCastLimitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition summon = SummonedUnitDefinition("summon");
        WorldSpellDefinition spell = SummonSpell(new WorldSpellDefinitionId("summon"), summon, WorldResourceSet.Empty);

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Grave"), new WorldPosition(2, 1), 1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventCastSpellMapEffect(spell.Id, location.Id, CasterId: null)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldStack summoned = SummonedStack(
            new WorldUnitId("Unit0001"),
            new WorldStackId("Stack0001"),
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            summon,
            new WorldPosition(2, 1));

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldSpellCastEvent(
                CasterId: null,
                spell.Id,
                new WorldSpellTarget(new WorldPosition(2, 1), Radius: 1),
                WorldResourceSet.Empty,
                WorldSpellCastSource.Scenario),
            new WorldStackCreatedEvent(summoned, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(100, 0, 0, 25, 0, 0),
                    TurnActions = new WorldPlayerTurnActions()
                }),
                Stacks = engine.State.Stacks.SetItem(summoned.Id, summoned),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CastSpellMapEffectSpreadOverLocationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition summon = SummonedUnitDefinition("summon");
        WorldSpellDefinition spell = SummonSpell(new WorldSpellDefinitionId("summon"), summon, WorldResourceSet.Empty);

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Grave"), new WorldPosition(2, 1), 1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventCastSpellMapEffect(spell.Id, location.Id, CasterId: null),
                new ScenarioEventCastSpellMapEffect(spell.Id, location.Id, CasterId: null),
                new ScenarioEventCastSpellMapEffect(spell.Id, location.Id, CasterId: null)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldStack first = SummonedStack(
            new WorldUnitId("Unit0001"),
            new WorldStackId("Stack0001"),
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            summon,
            new WorldPosition(2, 1));

        WorldStack second = SummonedStack(
            new WorldUnitId("Unit0002"),
            new WorldStackId("Stack0002"),
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            summon,
            new WorldPosition(1, 0));

        WorldStack third = SummonedStack(
            new WorldUnitId("Unit0003"),
            new WorldStackId("Stack0003"),
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            summon,
            new WorldPosition(2, 0));

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldSpellCastEvent(CasterId: null, spell.Id, new WorldSpellTarget(new WorldPosition(2, 1), Radius: 1), WorldResourceSet.Empty, WorldSpellCastSource.Scenario),
            new WorldStackCreatedEvent(first, WorldResourceSet.Empty),
            new WorldSpellCastEvent(CasterId: null, spell.Id, new WorldSpellTarget(new WorldPosition(2, 1), Radius: 1), WorldResourceSet.Empty, WorldSpellCastSource.Scenario),
            new WorldStackCreatedEvent(second, WorldResourceSet.Empty),
            new WorldSpellCastEvent(CasterId: null, spell.Id, new WorldSpellTarget(new WorldPosition(2, 1), Radius: 1), WorldResourceSet.Empty, WorldSpellCastSource.Scenario),
            new WorldStackCreatedEvent(third, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(first.Id, first)
                    .SetItem(second.Id, second)
                    .SetItem(third.Id, third),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CastSpellMapEffectStackInLocationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldSpellDefinition spell = TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId("restore"), new WorldRestoreMovementSpellEffect(50));

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(3, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Camp"), new WorldPosition(1, 1), 1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(location.Id)],
            [new ScenarioEventCastSpellMapEffect(spell.Id, location.Id, CasterId: null)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(2, 1)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(3, 1), new WorldPosition(2, 1), WorldStackDirection.NorthWest, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(3, 1), new WorldPosition(2, 1), WorldStackDirection.NorthWest, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldSpellCastEvent(
                    CasterId: null,
                    spell.Id,
                    new WorldSpellTarget(new WorldPosition(1, 1), Radius: 1),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Scenario),
                new WorldStackMovementRestoredEvent(stack.Id, 6),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(2, 1),
                    Direction = WorldStackDirection.NorthWest,
                    MovementPoints = 6
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void GiveSpellEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldSpellDefinition spell = TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId("Spell"), new WorldRestoreMovementSpellEffect(50)) with
        {
            CastingCost = new WorldResourceSet(0, 0, 0, 40, 0, 0)
        };
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventGiveSpellEffect(spell.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldSpellsReceivedEvent(player.Id, [spell.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(
                    player.Id,
                    player with
                    {
                        Resources = new WorldResourceSet(100, 0, 0, 25, 0, 0),
                        Spellbook = new WorldPlayerSpellbook([], [spell.Id])
                    }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void PopupEffectUnitPortraitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition portraitUnit = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("portraitUnit", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("Greetings Milord!"),
                    ScenarioEventPopupAudience.Triggerer,
                    ScenarioEventPortraitId.Unit(portraitUnit.Id),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(portraitUnit)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(
                scenarioEvent.Id,
                new ScenarioTextId("Greetings Milord!"),
                ScenarioEventPortraitId.Unit(portraitUnit.Id),
                PortraitName: null,
                IsPortraitLeft: true,
                player.Id,
                TriggererPosition: null,
                [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);
    }

    [Test]
    public void PopupEffectNamedPortraitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("Greetings Milord!"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("FYRWENN"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false)
            ],
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
            new WorldScenarioPopupEvent(
                scenarioEvent.Id,
                new ScenarioTextId("Greetings Milord!"),
                new ScenarioEventPortraitId("FYRWENN"),
                PortraitName: null,
                IsPortraitLeft: true,
                player.Id,
                TriggererPosition: null,
                [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);
    }

    [Test]
    public void ChangeOwnerEffectResetsOrderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer newOwner = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldSubrace newSubrace = new(
            new WorldSubraceId("undead"),
            newOwner.Id,
            WorldSubraceType.Undead,
            new WorldObjectVariant(5));
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

        WorldAiMemory memory = new(
            new WorldAiTurnMemory(1, player.Id),
            ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty.Add(
                stack.Id,
                new WorldAiStackMemory(
                    WorldStackOrder.Explore.Instance,
                    new WorldAiTarget.Location(new WorldPosition(2, 0)))));

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOwnerEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    newSubrace.Id,
                    FirstOnly: true,
                    PlayAnimation: true)
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
            .WithDefinition(TestHelpers.SimpleLord(newOwner.LordId, newOwner.Race, LordCategory.Warrior))
            .WithPlayer(newOwner)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithSubrace(newSubrace)
            .WithStack(stack)
            .WithAiMemory(player.Id, memory)
            .WithAiMemory(newOwner.Id, memory)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOwnerChangedEvent(stack.Id, newOwner.Id, newSubrace.Id, PlayAnimation: true),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    OwnerId = newOwner.Id,
                    SubraceId = newSubrace.Id,
                    Order = WorldStackOrder.Normal.Instance
                }),
                AiMemories = engine.State.AiMemories
                    .SetItem(player.Id, memory with { Stacks = ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty })
                    .SetItem(newOwner.Id, memory with { Stacks = ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOwnerEffectUnplayableRaceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer newOwner = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);
        WorldSubrace newSubrace = new(
            new WorldSubraceId("neutral"),
            newOwner.Id,
            WorldSubraceType.Neutral,
            new WorldObjectVariant(5));
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
                new ScenarioEventChangeOwnerEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    newSubrace.Id,
                    FirstOnly: true,
                    PlayAnimation: true)
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
            .WithDefinition(TestHelpers.SimpleLord(newOwner.LordId, newOwner.Race, LordCategory.Warrior))
            .WithPlayer(newOwner)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithSubrace(newSubrace)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOwnerChangedEvent(stack.Id, newOwner.Id, newSubrace.Id, PlayAnimation: true),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    OwnerId = newOwner.Id,
                    SubraceId = newSubrace.Id,
                    Order = WorldStackOrder.Stand.Instance
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void ChangeOwnerEffectSameOwnerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldSubrace newSubrace = new(
            new WorldSubraceId("subrace2"),
            player.Id,
            WorldSubraceType.Human,
            new WorldObjectVariant(5));
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

        WorldAiMemory memory = new(
            new WorldAiTurnMemory(1, player.Id),
            ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty.Add(
                stack.Id,
                new WorldAiStackMemory(
                    WorldStackOrder.Explore.Instance,
                    new WorldAiTarget.Location(new WorldPosition(2, 0)))));

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventChangeOwnerEffect(
                    new ScenarioStackReference.Stack(stack.Id),
                    newSubrace.Id,
                    FirstOnly: true,
                    PlayAnimation: true)
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
            .WithSubrace(newSubrace)
            .WithStack(stack)
            .WithAiMemory(player.Id, memory)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackOwnerChangedEvent(stack.Id, player.Id, newSubrace.Id, PlayAnimation: true),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    SubraceId = newSubrace.Id
                }),
                AiMemories = engine.State.AiMemories.SetItem(player.Id, memory),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void DefeatEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventWinEffect(WinnerId: null)],
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
            new WorldScenarioEndedEvent(WinnerId: null),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    IsEnded = true,
                    Winner = null
                }
            }));
    }

    [Test]
    public void DefeatEffectOnDestroyedStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack escort = TestHelpers.SimpleStack(
            new WorldStackId("Escort"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack rearguard = TestHelpers.SimpleStack(
            new WorldStackId("Rearguard"),
            player.Id,
            new WorldPosition(0, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldStack sentinel = TestHelpers.SimpleStack(
            new WorldStackId("Sentinel"),
            enemy.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human, WorldRace.Undead],
            [WorldRace.Human, WorldRace.Undead],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(escort.Id))],
            [new ScenarioEventWinEffect(WinnerId: null)],
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
            .WithStack(escort)
            .WithStack(rearguard)
            .WithStack(sentinel)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Defender, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        engine.CheckCommand(new WorldResolveStackBattleCommand(escort.Id, sentinel.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(
                escort.Id,
                sentinel.Id,
                BattleSide.Defender,
                UnitGrid<WorldUnit>.Empty,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader)),
            new WorldStackDestroyedEvent(escort.Id, new WorldUnitGroup.Stack(sentinel.Id), sentinel.OwnerId),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, enemy.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(sentinel.Id))),
            new WorldScenarioEndedEvent(WinnerId: null),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, enemy.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(sentinel.Id)))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(escort.Id, sentinel.OwnerId, SourceTemplateId: null)],
                    IsEnded = true,
                    Winner = null
                }
            }));
    }

    [Test]
    public void PopupEffectTriggererPortraitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition portraitUnit = TestHelpers.SimpleUnitDefinition("portraitUnit", maxHitpoints: 100, initiative: 50, damage: 10);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                .Build(),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("Ambush!"),
                    ScenarioEventPopupAudience.Triggerer,
                    ScenarioEventPortraitId.Unit(portraitUnit.Id),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(portraitUnit)
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
                new WorldScenarioPopupEvent(
                    scenarioEvent.Id,
                    new ScenarioTextId("Ambush!"),
                    ScenarioEventPortraitId.Unit(leader.Id),
                    "Konrad",
                    IsPortraitLeft: true,
                    player.Id,
                    TriggererPosition: new WorldPosition(0, 1),
                    [player.Id]),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);
    }

    [Test]
    public void PopupEffectImagePortraitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition portraitUnit = TestHelpers.SimpleUnitDefinition("portraitUnit", maxHitpoints: 100, initiative: 50, damage: 10);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                .Build(),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("Ambush!"),
                    ScenarioEventPopupAudience.Triggerer,
                    ScenarioEventPortraitId.Unit(portraitUnit.Id),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(portraitUnit)
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
                new WorldScenarioPopupEvent(
                    scenarioEvent.Id,
                    new ScenarioTextId("Ambush!"),
                    ScenarioEventPortraitId.Unit(portraitUnit.Id),
                    PortraitName: null,
                    IsPortraitLeft: true,
                    player.Id,
                    TriggererPosition: new WorldPosition(0, 1),
                    [player.Id]),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);
    }

    [Test]
    public void PopupEffectNoPortraitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition portraitUnit = TestHelpers.SimpleUnitDefinition("portraitUnit", maxHitpoints: 100, initiative: 50, damage: 10);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                .Build(),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("Ambush!"),
                    ScenarioEventPopupAudience.Triggerer,
                    null,
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(portraitUnit)
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
                new WorldScenarioPopupEvent(
                    scenarioEvent.Id,
                    new ScenarioTextId("Ambush!"),
                    PortraitId: null,
                    PortraitName: null,
                    IsPortraitLeft: true,
                    player.Id,
                    TriggererPosition: new WorldPosition(0, 1),
                    [player.Id]),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);
    }

    [Test]
    public void PopupEffectTriggererPortraitWithoutStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition portraitUnit = TestHelpers.SimpleUnitDefinition("portraitUnit", maxHitpoints: 100, initiative: 50, damage: 10);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("Greetings Milord!"),
                    ScenarioEventPopupAudience.Triggerer,
                    ScenarioEventPortraitId.Unit(portraitUnit.Id),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(portraitUnit)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(
                scenarioEvent.Id,
                new ScenarioTextId("Greetings Milord!"),
                ScenarioEventPortraitId.Unit(portraitUnit.Id),
                PortraitName: null,
                IsPortraitLeft: true,
                player.Id,
                TriggererPosition: null,
                [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);
    }

    [Test]
    public void PopupEffectAffectedRacesAudienceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Neutral, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human, WorldRace.Neutral],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.AffectedRaces, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
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
    }

    [Test]
    public void PopupEffectDefeatedPlayerAudienceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
                new WorldFortificationId("Capital2"),
                enemy.Id,
                new WorldPosition(6, 6),
                UnitGrid<WorldUnit>.Empty) with
            {
                CapturedById = player.Id
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.AllPlayers, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithCapital(capital)
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
    }

    [Test]
    public void CreateStackEffectAfterRemoveStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ItemDefinition itemDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("Item"), ItemCategory.Valuable);
        WorldItem item = new(new WorldItemId("Item1"), itemDefinition.Id, Charges: null);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(1, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("Unit0001"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([item])
            };

        ScenarioStackTemplateDefinition template = new(
            templateId,
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(leader),
            WorldStackOrder.Normal.Instance,
            Direction: null,
            AiPriority: 3);

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Camp"), new ScenarioTextId("Camp"), new WorldPosition(1, 0), 1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Stack(stack.Id), FirstOnly: true),
                new ScenarioEventCreateStackEffect(templateId, location.Id)
            ],
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
            .WithScenarioStackTemplate(template)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldStack created = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRemovedEvent(stack.Id),
            new WorldStackCreatedEvent(created, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(created.Id, created),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty.Add(
                    new WorldBagId("Bag0001"),
                    new WorldBag(
                        new WorldBagId("Bag0001"),
                        new WorldPosition(1, 0),
                        new WorldObjectVariant(0),
                        new WorldInventory([item]),
                        AiPriority: 3)),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(stack.Id, DestroyerPlayerId: null, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void CastSpellMapEffectBagInLocationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition summon = SummonedUnitDefinition("summon");
        WorldSpellDefinition spell = SummonSpell(new WorldSpellDefinitionId("summon"), summon, WorldResourceSet.Empty);
        ItemDefinition itemDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("Item"), ItemCategory.Valuable);

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(1, 1),
            new WorldObjectVariant(0),
            new WorldInventory([new WorldItem(new WorldItemId("Item1"), itemDefinition.Id, Charges: null)]),
            AiPriority: 3);

        ScenarioLocationDefinition location = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Grave"), new WorldPosition(1, 1), 1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventCastSpellMapEffect(spell.Id, location.Id, CasterId: null)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithBag(bag)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldStack summoned = SummonedStack(
            new WorldUnitId("Unit0001"),
            new WorldStackId("Stack0001"),
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            summon,
            new WorldPosition(0, 0));

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldSpellCastEvent(CasterId: null, spell.Id, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 1), WorldResourceSet.Empty, WorldSpellCastSource.Scenario),
            new WorldStackCreatedEvent(summoned, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(summoned.Id, summoned),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty.Add(bag.Id, bag),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void BattleEffectIllusionTriggererTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 6,
            Lifetime = 3
        };
        UnitDefinition raider = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("raider", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 6) with
            {
                ExpirationTurn = 3
            };

        WorldStack ambush = TestHelpers.SimpleStack(
            new WorldStackId("Ambush"),
            enemy.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("raider"), raider),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventBattleEffect(new ScenarioStackReference.Stack(ambush.Id))],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(illusion)
            .WithDefinition(raider)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(ambush)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(
                    stack.Id,
                    new WorldPosition(0, 0),
                    new WorldPosition(0, 1),
                    WorldStackDirection.SouthWest,
                    4,
                    [], ambush.Position),
                new WorldStackMovedEvent(
                    stack.Id,
                    new WorldPosition(0, 0),
                    new WorldPosition(0, 1),
                    WorldStackDirection.SouthWest,
                    4,
                    [], ambush.Position),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldStackDestroyedEvent(stack.Id, new WorldUnitGroup.Stack(ambush.Id), enemy.Id),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.Remove(stack.Id),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(stack.Id, enemy.Id, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void BattleEffectIllusionAttackerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition illusion = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Illusion,
            Leadership = 1,
            MovementPoints = 6,
            Lifetime = 3
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack ambush = TestHelpers.SimpleStack(
                new WorldStackId("Ambush"),
                enemy.Id,
                new WorldPosition(2, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("illusion"), illusion),
                movementPoints: 6) with
            {
                ExpirationTurn = 3
            };

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Ambush"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventBattleEffect(new ScenarioStackReference.Stack(ambush.Id))],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(illusion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(ambush)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(0, 1)),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(0, 0), new WorldPosition(0, 1), WorldStackDirection.SouthWest, 4, [], null),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
                new WorldStackRelocatedEvent(ambush.Id, new WorldPosition(2, 2), new WorldPosition(1, 1), WorldStackDirection.NorthWest),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks
                    .SetItem(stack.Id, stack with
                    {
                        Position = new WorldPosition(0, 1),
                        Direction = WorldStackDirection.SouthWest,
                        MovementPoints = 4
                    })
                    .SetItem(ambush.Id, ambush with
                    {
                        Position = new WorldPosition(1, 1),
                        Direction = WorldStackDirection.NorthWest
                    }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void CreateStackEffectWalksCreatedStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);
        UnitDefinition dragon = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dragon", maxHitpoints: 900, initiative: 50, damage: 100),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Dragon");

        ScenarioStackTemplateDefinition template = new(
            templateId,
            neutral.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(dragon),
            WorldStackOrder.Normal.Instance,
            Direction: null,
            AiPriority: 4);

        ScenarioLocationDefinition lair = new(new ScenarioLocationId("Lair"), new ScenarioTextId("Lair"), new WorldPosition(2, 0), 0);
        ScenarioLocationDefinition clearing = new(new ScenarioLocationId("Clearing"), new ScenarioTextId("Clearing"), new WorldPosition(4, 0), 0);

        ScenarioEventDefinition summonEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Elf],
            [WorldRace.Elf],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventCreateStackEffect(templateId, lair.Id),
                new ScenarioEventMoveStackEffect(new ScenarioStackReference.Template(templateId), clearing.Id),
                new ScenarioEventEnableEventEffect(new ScenarioEventId("Event2"), Enabled: true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);
        ScenarioEventDefinition arrivalEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Elf],
            [WorldRace.Elf, WorldRace.Neutral],
            [new ScenarioEventStackToLocationCondition(new ScenarioStackReference.Template(templateId), clearing.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The dragon breaks loose."), ScenarioEventPopupAudience.AllPlayers, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: false,
            OccurOnce: true,
            Chance: 100,
            Order: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dragon)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithPlayer(neutral)
            .WithScenarioLocation(lair)
            .WithScenarioLocation(clearing)
            .WithScenarioStackTemplate(template)
            .WithScenarioEvent(summonEvent)
            .WithScenarioEvent(arrivalEvent)
            .Build();

        WorldStackId createdId = new("Stack0001");

        WorldStack created = TestHelpers.SimpleStack(
                createdId,
                neutral.Id,
                new WorldPosition(2, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), dragon) with { Name = "Konrad" }, GridPosition.FrontBottom, dragon.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                AiPriority = 4,
                Order = WorldStackOrder.Roam.Instance,
                SourceTemplateId = templateId
            };

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 0, 0, 25)),
            new WorldScenarioEventTriggeringEvent(summonEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackCreatedEvent(created, WorldResourceSet.Empty),
            new WorldStackRelocatedEvent(createdId, new WorldPosition(2, 0), new WorldPosition(3, 0), WorldStackDirection.SouthEast),
            new WorldStackRelocatedEvent(createdId, new WorldPosition(3, 0), new WorldPosition(4, 0), WorldStackDirection.SouthEast),
            new WorldScenarioEventTriggeringEvent(arrivalEvent.Id, neutral.Id, new ScenarioEventStackCreatedTrigger(createdId)),
            new WorldScenarioPopupEvent(arrivalEvent.Id, new ScenarioTextId("The dragon breaks loose."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, neutral.Id, TriggererPosition: new WorldPosition(4, 0), [player.Id, neutral.Id]),
            new WorldScenarioEventTriggeredEvent(arrivalEvent.Id, neutral.Id, new ScenarioEventStackCreatedTrigger(createdId)),
            new WorldScenarioEventTriggeredEvent(summonEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(createdId, created with
                {
                    Position = new WorldPosition(4, 0),
                    Direction = WorldStackDirection.SouthEast
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty
                        .Add(summonEvent.Id, false)
                        .Add(arrivalEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            arrivalEvent.Id,
                            new ScenarioTextId("The dragon breaks loose."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            neutral.Id,
                            [player.Id, neutral.Id])
                    ]
                }
            }));
    }

    [Test]
    public void CastSpellEffectAttackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldSpellDefinition spell = TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId("attack"), new WorldAttackSpellEffect(UnitAttackSource.Fire, Damage: 10));

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(new ScenarioLocationId("Zone1"), new ScenarioTextId("Zone"), new WorldPosition(0, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventEnteringZoneCondition(zone.Id)],
            [new ScenarioEventCastSpellEffect(spell.Id, CasterId: null)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
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
                new WorldSpellCastEvent(
                    CasterId: null,
                    spell.Id,
                    new WorldSpellTarget(new WorldPosition(0, 1), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Scenario),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"), 90, SourcePlayerId: null),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
            ]);
    }
}
