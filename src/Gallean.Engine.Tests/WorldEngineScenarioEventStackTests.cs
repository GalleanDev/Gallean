using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineScenarioEventStackTests
{
    [Test]
    public void StackToLocationOtherStackTest()
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

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Destination"),
            new WorldPosition(0, 0),
            1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackToLocationCondition(new ScenarioStackReference.Stack(new WorldStackId("Chosen")), location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The chosen one arrives."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
    public void StackToLocationTest()
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

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Destination"),
            new WorldPosition(0, 0),
            1);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackToLocationCondition(new ScenarioStackReference.Stack(stack.Id), location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The chosen one arrives."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The chosen one arrives."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(1, 1), [player.Id]),
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
    public void StackToLocationOtherStackMovesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack escort = TestHelpers.SimpleStack(
            new WorldStackId("Escort"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack scout = TestHelpers.SimpleStack(
            new WorldStackId("Scout"),
            player.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Destination"),
            new WorldPosition(0, 0),
            0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackToLocationCondition(new ScenarioStackReference.Stack(escort.Id), location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The chosen one arrives."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(escort)
            .WithStack(scout)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldMoveStackCommand(scout.Id, new WorldPosition(2, 1)));

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
    public void StackToLocationTemplateTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack escort = TestHelpers.SimpleStack(
                new WorldStackId("Escort"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        WorldStack scout = TestHelpers.SimpleStack(
            new WorldStackId("Scout"),
            player.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Destination"),
            new WorldPosition(0, 0),
            0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackToLocationCondition(new ScenarioStackReference.Template(templateId), location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The chosen one arrives."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(escort)
            .WithStack(scout)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldMoveStackCommand(scout.Id, new WorldPosition(2, 1)));

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
    public void StackToLocationTemplateOtherPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack escort = TestHelpers.SimpleStack(
                new WorldStackId("Escort"),
                enemy.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        WorldStack scout = TestHelpers.SimpleStack(
            new WorldStackId("Scout"),
            player.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Destination"),
            new WorldPosition(0, 0),
            0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackToLocationCondition(new ScenarioStackReference.Template(templateId), location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The chosen one arrives."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithStack(scout)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldMoveStackCommand(scout.Id, new WorldPosition(2, 1)));

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void StackExistenceTest()
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
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Stack(stack.Id), MustExist: true)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The guide still lives."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void StackExistenceMissingStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Stack(new WorldStackId("Missing")), MustExist: true)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The guide still lives."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void StackAbsenceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
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
    public void StackAbsenceLivingStackTest()
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
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Stack(stack.Id), MustExist: false)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The Bone Lord was destroyed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void StackAbsenceAfterBattleTest()
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

        WorldStack boneLord = TestHelpers.SimpleStack(
            new WorldStackId("BoneLord"),
            enemy.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldStack enemyRearguard = TestHelpers.SimpleStack(
            new WorldStackId("Rearguard"),
            enemy.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Stack(boneLord.Id), MustExist: false)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The Bone Lord was destroyed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithStack(boneLord)
            .WithStack(enemyRearguard)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));

        BattleUnit survivor = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("leader"),
            DefinitionId: leader.Id,
            Side: BattleSide.Attacker,
            Hitpoints: leader.MaxHitpoints,
            Experience: 0,
            ExperienceGained: 0,
            Level: leader.Level,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(survivor, GridPosition.FrontBottom, isLarge: false).Build(),
            DefenderGrid: UnitGrid<BattleUnit>.Empty,
            CurrentUnitTurn: new BattleUnitTurn(survivor.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        _ = engine.DispatchCommand(new WorldResolveStackBattleCommand(stack.Id, boneLord.Id, finalBattleState, IsDuel: false));
        _ = engine.DispatchCommand(new WorldEndTurnCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(boneLord.Id, stack.OwnerId, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void DestroyingStackTest()
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

        WorldStack boneLord = TestHelpers.SimpleStack(
            new WorldStackId("BoneLord"),
            enemy.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldStack enemyRearguard = TestHelpers.SimpleStack(
            new WorldStackId("Rearguard"),
            enemy.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(boneLord.Id))],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The Bone Lord was destroyed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithStack(boneLord)
            .WithStack(enemyRearguard)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack.Id, boneLord.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(
                stack.Id,
                boneLord.Id,
                BattleSide.Attacker,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                UnitGrid<WorldUnit>.Empty),
            new WorldStackDestroyedEvent(boneLord.Id, new WorldUnitGroup.Stack(stack.Id), stack.OwnerId),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id))),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The Bone Lord was destroyed."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(0, 0), [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id)))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(boneLord.Id, stack.OwnerId, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void DestroyingStackKilledByDefenderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack raider = TestHelpers.SimpleStack(
            new WorldStackId("Raider"),
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
            [WorldRace.Undead],
            [WorldRace.Undead],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(raider.Id))],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The raider was cut down."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithStack(raider)
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

        engine.CheckCommand(new WorldResolveStackBattleCommand(raider.Id, sentinel.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(
                raider.Id,
                sentinel.Id,
                BattleSide.Defender,
                UnitGrid<WorldUnit>.Empty,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader)),
            new WorldStackDestroyedEvent(raider.Id, new WorldUnitGroup.Stack(sentinel.Id), sentinel.OwnerId),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, enemy.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(sentinel.Id))),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The raider was cut down."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, enemy.Id, TriggererPosition: new WorldPosition(2, 0), [enemy.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, enemy.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(sentinel.Id)))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(raider.Id, sentinel.OwnerId, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void DestroyingStackAtFortificationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map);
        WorldPlayer elf = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Elf, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack dragon = TestHelpers.SimpleStack(
            new WorldStackId("Dragon"),
            neutral.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Elf, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            elf.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader));

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Elf],
            [WorldRace.Neutral, WorldRace.Elf],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(dragon.Id))],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The dragon is slain."), ScenarioEventPopupAudience.AllPlayers, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(elf.LordId, elf.Race, LordCategory.Warrior))
            .WithPlayer(elf)
            .WithStack(dragon)
            .WithCity(city)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Defender, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        engine.CheckCommand(new WorldResolveFortificationBattleCommand(dragon.Id, city.Id, finalBattleState), [
            new WorldFortificationBattleEndedEvent(dragon.Id, city.Id, BattleSide.Defender, UnitGrid<WorldUnit>.Empty, TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader)),
            new WorldStackDestroyedEvent(dragon.Id, new WorldUnitGroup.Fortification(city.Id), elf.Id),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, neutral.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Fortification(city.Id))),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The dragon is slain."), null, PortraitName: null, IsPortraitLeft: true, neutral.Id, TriggererPosition: null, [neutral.Id, elf.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, neutral.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Fortification(city.Id))),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, elf.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Fortification(city.Id))),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The dragon is slain."), null, PortraitName: null, IsPortraitLeft: true, elf.Id, TriggererPosition: null, [neutral.Id, elf.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, elf.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Fortification(city.Id)))
        ]);
    }

    [Test]
    public void DestroyingStackNotPlacedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("Missing")))],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The Bone Lord was destroyed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void DestroyingStackMultipleTest()
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

        WorldStack firstGuard = TestHelpers.SimpleStack(
            new WorldStackId("Guard1"),
            enemy.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldStack secondGuard = TestHelpers.SimpleStack(
            new WorldStackId("Guard2"),
            enemy.Id,
            new WorldPosition(2, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            movementPoints: 6);

        WorldStack enemyRearguard = TestHelpers.SimpleStack(
            new WorldStackId("Rearguard"),
            enemy.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader4"), leader),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [
                new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(firstGuard.Id)),
                new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(secondGuard.Id))
            ],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Both guards were destroyed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithStack(firstGuard)
            .WithStack(secondGuard)
            .WithStack(enemyRearguard)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack.Id, firstGuard.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(
                stack.Id,
                firstGuard.Id,
                BattleSide.Attacker,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                UnitGrid<WorldUnit>.Empty),
            new WorldStackDestroyedEvent(firstGuard.Id, new WorldUnitGroup.Stack(stack.Id), stack.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    DestroyedStacks = [new ScenarioDestroyedStack(firstGuard.Id, stack.OwnerId, SourceTemplateId: null)]
                }
            }));

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack.Id, secondGuard.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(
                stack.Id,
                secondGuard.Id,
                BattleSide.Attacker,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                UnitGrid<WorldUnit>.Empty),
            new WorldStackDestroyedEvent(secondGuard.Id, new WorldUnitGroup.Stack(stack.Id), stack.OwnerId),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id))),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Both guards were destroyed."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(0, 0), [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id)))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks =
                    [
                        new ScenarioDestroyedStack(firstGuard.Id, stack.OwnerId, SourceTemplateId: null),
                        new ScenarioDestroyedStack(secondGuard.Id, stack.OwnerId, SourceTemplateId: null)
                    ]
                }
            }));
    }

    [Test]
    public void TemplateWithoutInstancesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        ScenarioStackTemplateId templateId = new("Template1");

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Template(templateId), MustExist: false)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The raiders are gone."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void TemplateExistenceWithoutInstancesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        ScenarioStackTemplateId templateId = new("Template1");

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Template(templateId), MustExist: true)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The raiders are here."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void TemplateExistenceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId templateId = new("Template1");

        WorldStack raider = TestHelpers.SimpleStack(
                new WorldStackId("Raider1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Template(templateId), MustExist: true)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The raiders are here."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
    public void TemplateAbsenceAfterInstanceDestroyedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

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

        WorldStack raider = TestHelpers.SimpleStack(
                new WorldStackId("Raider1"),
                enemy.Id,
                new WorldPosition(1, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Template(templateId), MustExist: false)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The raiders are gone."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithStack(raider)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack.Id, raider.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(
                stack.Id,
                raider.Id,
                BattleSide.Attacker,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                UnitGrid<WorldUnit>.Empty),
            new WorldStackDestroyedEvent(raider.Id, new WorldUnitGroup.Stack(stack.Id), stack.OwnerId),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id))),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The raiders are gone."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(0, 0), [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id)))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(raider.Id, stack.OwnerId, templateId)]
                }
            }));
    }

    [Test]
    public void DestroyingStackRemovedTest()
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

        ScenarioEventDefinition removeEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Stack(stack.Id), FirstOnly: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        ScenarioEventDefinition destroyedEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(stack.Id))],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The stack was destroyed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithScenarioEvent(removeEvent)
            .WithScenarioEvent(destroyedEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(removeEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(stack.Id, DestroyerPlayerId: null, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void ChainedEventSeesRemovedStackTest()
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

        ScenarioEventDefinition removeEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Stack(stack.Id), FirstOnly: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        ScenarioEventDefinition chainedEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Stack(stack.Id), MustExist: false)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The stack is gone."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithScenarioEvent(removeEvent)
            .WithScenarioEvent(chainedEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(removeEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRemovedEvent(stack.Id),
            new WorldScenarioEventTriggeringEvent(chainedEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(null)),
            new WorldScenarioPopupEvent(chainedEvent.Id, new ScenarioTextId("The stack is gone."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(chainedEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(null)),
            new WorldScenarioEventTriggeredEvent(removeEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.Remove(stack.Id),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(removeEvent.Id, false).Add(chainedEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(stack.Id, DestroyerPlayerId: null, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void DestroyingStackRemovedForNeutralsTest()
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

        ScenarioEventDefinition removeEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Stack(stack.Id), FirstOnly: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        ScenarioEventDefinition destroyedEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human, WorldRace.Neutral],
            [WorldRace.Human],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(stack.Id))],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The stack was destroyed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithScenarioEvent(removeEvent)
            .WithScenarioEvent(destroyedEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(removeEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRemovedEvent(stack.Id),
            new WorldScenarioEventTriggeringEvent(destroyedEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(null)),
            new WorldScenarioPopupEvent(destroyedEvent.Id, new ScenarioTextId("The stack was destroyed."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(destroyedEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(null)),
            new WorldScenarioEventTriggeredEvent(removeEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(removeEvent.Id, false).Add(destroyedEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(stack.Id, DestroyerPlayerId: null, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void DestroyingStackKilledByUnaffectedRaceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack raider = TestHelpers.SimpleStack(
            new WorldStackId("Raider"),
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
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(raider.Id))],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The raider was cut down."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithStack(raider)
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

        _ = engine.DispatchCommand(new WorldResolveStackBattleCommand(raider.Id, sentinel.Id, finalBattleState, IsDuel: false));
        _ = engine.DispatchCommand(new WorldEndTurnCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    DestroyedStacks = [new ScenarioDestroyedStack(raider.Id, enemy.Id, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void DestroyingTemplateInstanceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

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

        WorldStack raider = TestHelpers.SimpleStack(
                new WorldStackId("Raider1"),
                enemy.Id,
                new WorldPosition(1, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = templateId
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Template(templateId))],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The raiders were slain."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithStack(raider)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack.Id, raider.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(
                stack.Id,
                raider.Id,
                BattleSide.Attacker,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                UnitGrid<WorldUnit>.Empty),
            new WorldStackDestroyedEvent(raider.Id, new WorldUnitGroup.Stack(stack.Id), stack.OwnerId),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id))),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The raiders were slain."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(0, 0), [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id)))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(raider.Id, stack.OwnerId, templateId)]
                }
            }));
    }

    [Test]
    public void DestroyingTemplateCreatedAfterDeathTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId raidersId = new("Raiders");
        ScenarioStackTemplateId championId = new("Champion");

        WorldStack hero = TestHelpers.SimpleStack(
            new WorldStackId("Hero"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack raider = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                enemy.Id,
                new WorldPosition(1, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = raidersId
            };

        ScenarioStackTemplateDefinition champion = new(
            championId,
            enemy.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(leader),
            WorldStackOrder.Normal.Instance,
            Direction: null,
            AiPriority: 3);

        ScenarioLocationDefinition lair = new(new ScenarioLocationId("Lair"), new ScenarioTextId("Lair"), new WorldPosition(2, 0), 0);

        ScenarioEventDefinition championEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Template(raidersId))],
            [new ScenarioEventCreateStackEffect(championId, lair.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        ScenarioEventDefinition championDestroyedEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Template(championId))],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The champion has fallen."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithStack(hero)
            .WithStack(raider)
            .WithScenarioLocation(lair)
            .WithScenarioStackTemplate(champion)
            .WithScenarioEvent(championEvent)
            .WithScenarioEvent(championDestroyedEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        WorldStack championStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack0002"),
                enemy.Id,
                new WorldPosition(2, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                SourceTemplateId = championId
            };

        engine.CheckCommand(new WorldResolveStackBattleCommand(hero.Id, raider.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(
                hero.Id,
                raider.Id,
                BattleSide.Attacker,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                UnitGrid<WorldUnit>.Empty),
            new WorldStackDestroyedEvent(raider.Id, new WorldUnitGroup.Stack(hero.Id), hero.OwnerId),
            new WorldScenarioEventTriggeringEvent(championEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(hero.Id))),
            new WorldStackCreatedEvent(championStack, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeredEvent(championEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(hero.Id)))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(championEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(raider.Id, hero.OwnerId, raidersId)]
                }
            }));
    }

    [Test]
    public void TemplateAbsenceAfterInstanceRemovedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        ScenarioStackTemplateId scoutsId = new("Scouts");
        ScenarioStackTemplateId escortId = new("Escort");

        WorldStack scout = TestHelpers.SimpleStack(
                new WorldStackId("Stack0001"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("Unit0001"), leader),
                movementPoints: 6) with
            {
                SourceTemplateId = scoutsId
            };

        ScenarioStackTemplateDefinition escort = new(
            escortId,
            player.Id,
            TestHelpers.NeutralSubrace.Id,
            TestHelpers.SingleTemplateUnitGrid(leader),
            WorldStackOrder.Normal.Instance,
            Direction: null,
            AiPriority: 3);

        ScenarioLocationDefinition camp = new(new ScenarioLocationId("Camp"), new ScenarioTextId("Camp"), new WorldPosition(2, 0), 0);

        ScenarioEventDefinition replaceEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Template(scoutsId), FirstOnly: false),
                new ScenarioEventCreateStackEffect(escortId, camp.Id)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        ScenarioEventDefinition absenceEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Template(scoutsId), MustExist: false)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The scouts are gone."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(scout)
            .WithScenarioLocation(camp)
            .WithScenarioStackTemplate(escort)
            .WithScenarioEvent(replaceEvent)
            .WithScenarioEvent(absenceEvent)
            .Build();

        WorldStack escortStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack0002"),
                player.Id,
                new WorldPosition(2, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), leader) with { Name = "Konrad" }, GridPosition.FrontBottom, leader.IsLarge)
                    .Build(),
                movementPoints: 6) with
            {
                SourceTemplateId = escortId
            };

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(scout.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(replaceEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRemovedEvent(scout.Id),
            new WorldStackCreatedEvent(escortStack, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeringEvent(absenceEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(null)),
            new WorldScenarioPopupEvent(absenceEvent.Id, new ScenarioTextId("The scouts are gone."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]),
            new WorldScenarioEventTriggeredEvent(absenceEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(null)),
            new WorldScenarioEventTriggeredEvent(replaceEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(escortStack.Id, escortStack),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(replaceEvent.Id, false).Add(absenceEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(scout.Id, DestroyerPlayerId: null, scoutsId)]
                }
            }));
    }

    [Test]
    public void DestroyingStackUnaffectedKillerTest()
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

        WorldStack boneLord = TestHelpers.SimpleStack(
            new WorldStackId("BoneLord"),
            enemy.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldStack enemyRearguard = TestHelpers.SimpleStack(
            new WorldStackId("Rearguard"),
            enemy.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            movementPoints: 6);

        ScenarioEventDefinition rivalEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Heretic],
            [WorldRace.Human],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(boneLord.Id))],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The Legions claim the kill."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event2"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(boneLord.Id))],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The Bone Lord was destroyed."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithStack(boneLord)
            .WithStack(enemyRearguard)
            .WithScenarioEvent(rivalEvent)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack.Id, boneLord.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(
                stack.Id,
                boneLord.Id,
                BattleSide.Attacker,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                UnitGrid<WorldUnit>.Empty),
            new WorldStackDestroyedEvent(boneLord.Id, new WorldUnitGroup.Stack(stack.Id), stack.OwnerId),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id))),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The Bone Lord was destroyed."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(0, 0), [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackDestroyedTrigger(new WorldUnitGroup.Stack(stack.Id)))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(boneLord.Id, stack.OwnerId, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void StackToLocationCityTest()
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

        WorldStack escort = TestHelpers.SimpleStack(
            new WorldStackId("Escort"),
            player.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack scout = TestHelpers.SimpleStack(
            new WorldStackId("Scout"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Destination"),
            new WorldPosition(3, 3),
            0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackToLocationCondition(new ScenarioStackReference.Stack(escort.Id), location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The chosen one arrives."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithStack(escort)
            .WithStack(scout)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldMoveStackCommand(scout.Id, new WorldPosition(5, 4)));

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void StackToLocationCapturedCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldCapital capital = TestHelpers.SimpleCapital(
                new WorldFortificationId("Capital1"),
                enemy.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                CapturedById = player.Id
            };

        WorldStack escort = TestHelpers.SimpleStack(
            new WorldStackId("Escort"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack scout = TestHelpers.SimpleStack(
            new WorldStackId("Scout"),
            player.Id,
            new WorldPosition(6, 6),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Destination"),
            new WorldPosition(4, 4),
            0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackToLocationCondition(new ScenarioStackReference.Stack(escort.Id), location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The chosen one arrives."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithCapital(capital)
            .WithStack(escort)
            .WithStack(scout)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldMoveStackCommand(scout.Id, new WorldPosition(6, 5)));

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
    public void StackToLocationRuinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Resources: WorldResourceSet.Empty,
            Item: null,
            LooterId: player.Id,
            AiPriority: 3);

        WorldStack escort = TestHelpers.SimpleStack(
            new WorldStackId("Escort"),
            player.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack scout = TestHelpers.SimpleStack(
            new WorldStackId("Scout"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition location = new(
            new ScenarioLocationId("Location1"),
            new ScenarioTextId("Destination"),
            new WorldPosition(2, 2),
            0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventStackToLocationCondition(new ScenarioStackReference.Stack(escort.Id), location.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The chosen one arrives."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRuin(ruin)
            .WithStack(escort)
            .WithStack(scout)
            .WithScenarioLocation(location)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldMoveStackCommand(scout.Id, new WorldPosition(5, 4)));

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
