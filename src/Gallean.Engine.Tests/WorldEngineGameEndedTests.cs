using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineGameEndedTests
{
    [Test]
    public void DefeatSkipsTurnsTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer bystander = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

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
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, capital.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("capital")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(attacker.Id),
            new WorldTurnEndedEvent(defender.Id),
            new WorldTurnStartingEvent(bystander.Id),
            new WorldTurnStartedEvent(bystander.Id),
            new WorldTurnResourcesGeneratedEvent(bystander.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = capital.Entrance,
                Direction = WorldStackDirection.NorthWest,
                MovementPoints = 2
            }),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
            {
                CapturedById = attacker.Id
            }),
            TurnInfo = new WorldTurnInfo(1, bystander.Id),
            ScenarioState = WorldScenarioState.Empty
        }));
    }

    [Test]
    public void LastHumanDefeatTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer bystander = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map) with { IsAi = true };
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

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
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, capital.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("capital")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldScenarioEndedEvent(null),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = capital.Entrance,
                Direction = WorldStackDirection.NorthWest,
                MovementPoints = 2
            }),
            Players = engine.State.Players.SetItem(defender.Id, defender with
            {
                Resources = new WorldResourceSet(100, 0, 0, 0, 25, 0)
            }),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
            {
                CapturedById = attacker.Id
            }),
            ScenarioState = WorldScenarioState.Empty with
            {
                IsEnded = true
            }
        }));
    }

    [Test]
    public void AllAiPlayersTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };
        WorldPlayer bystander = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map) with { IsAi = true };
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

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
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, capital.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("capital")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = capital.Entrance,
                Direction = WorldStackDirection.NorthWest,
                MovementPoints = 2
            }),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
            {
                CapturedById = attacker.Id
            }),
            ScenarioState = WorldScenarioState.Empty
        }));
    }

    [Test]
    public void DefeatDestroysStacksTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^...."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack attackerStack = TestHelpers.SimpleStack(
            new WorldStackId("attacker"),
            attacker.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldStack defenderStack = TestHelpers.SimpleStack(
            new WorldStackId("defender"),
            defender.Id,
            new WorldPosition(7, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 2);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(attackerStack)
            .WithStack(defenderStack)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(attackerStack.Id, capital.Entrance), [
            new WorldStackMovingEvent(attackerStack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("attacker"), new WorldFortificationId("capital")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldStackDestroyedEvent(defenderStack.Id, Destroyer: null, DestroyerPlayerId: null),
            new WorldScenarioEndedEvent(attacker.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldStackMovedEvent(attackerStack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(attackerStack.Id, attackerStack with
                {
                    Position = capital.Entrance,
                    Direction = WorldStackDirection.NorthWest,
                    MovementPoints = 2
                })
                .Remove(defenderStack.Id),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with { CapturedById = attacker.Id }),
            ScenarioState = WorldScenarioState.Empty with
            {
                DestroyedStacks = [new ScenarioDestroyedStack(defenderStack.Id, DestroyerPlayerId: null, SourceTemplateId: null)],
                IsEnded = true,
                Winner = attacker.Id
            }
        }));
    }

    [Test]
    public void DefeatDestroysGuardTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 6);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack attackerStack = TestHelpers.SimpleStack(
            new WorldStackId("attacker"),
            attacker.Id,
            new WorldPosition(5, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldStack guardStack = TestHelpers.SimpleStack(
            new WorldStackId("guard"),
            defender.Id,
            new WorldPosition(3, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 2);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(attackerStack)
            .WithStack(guardStack)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(attackerStack.Id, capital.Entrance), [
            new WorldStackMovingEvent(attackerStack.Id, new WorldPosition(5, 3), capital.Entrance, WorldStackDirection.West, 2, [], guardStack.Position),
            new WorldFortificationEnteredEvent(attackerStack.Id, capital.Id),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldStackDestroyedEvent(guardStack.Id, Destroyer: null, DestroyerPlayerId: null),
            new WorldScenarioEndedEvent(attacker.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldStackMovedEvent(attackerStack.Id, new WorldPosition(5, 3), capital.Entrance, WorldStackDirection.West, 2, [], guardStack.Position)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(attackerStack.Id, attackerStack with
                {
                    Position = capital.Entrance,
                    Direction = WorldStackDirection.West,
                    MovementPoints = 2
                })
                .Remove(guardStack.Id),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with { CapturedById = attacker.Id }),
            ScenarioState = WorldScenarioState.Empty with
            {
                DestroyedStacks = [new ScenarioDestroyedStack(guardStack.Id, DestroyerPlayerId: null, SourceTemplateId: null)],
                IsEnded = true,
                Winner = attacker.Id
            }
        }));
    }

    [Test]
    public void SecondCapitalNoDefeatTest()
    {
        WorldMap map = TestHelpers.SimpleMap(11, 5);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldCapital capturedCapital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital1"),
            defender.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);
        WorldCapital remainingCapital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital2"),
            defender.Id,
            new WorldPosition(10, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCapital(capturedCapital)
            .WithCapital(remainingCapital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, capturedCapital.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 4), capturedCapital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("capital1")),
            new WorldFortificationCapturingEvent(capturedCapital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldFortificationCapturedEvent(capturedCapital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 4), capturedCapital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = capturedCapital.Entrance,
                Direction = WorldStackDirection.NorthWest,
                MovementPoints = 2
            }),
            Fortifications = engine.State.Fortifications.SetItem(capturedCapital.Id, capturedCapital with
            {
                CapturedById = attacker.Id
            }),
            ScenarioState = WorldScenarioState.Empty
        }));
        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "...........",
            "...........",
            "...........",
            "...........",
            "..........."
        ]));
    }

    [Test]
    public void WinnerAlreadyDeclaredTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Undead],
            [WorldRace.Undead],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventWinEffect(attacker.Id)],
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
            .WithStack(stack)
            .WithCapital(capital)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, capital.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("capital")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = capital.Entrance,
                Direction = WorldStackDirection.NorthWest,
                MovementPoints = 2
            }),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
            {
                CapturedById = attacker.Id
            }),
            ScenarioState = WorldScenarioState.Empty with
            {
                EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                IsEnded = true,
                Winner = attacker.Id
            }
        }));
    }

    [Test]
    public void ScriptedWinOnDefeatTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^...."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack attackerStack = TestHelpers.SimpleStack(
            new WorldStackId("attacker"),
            attacker.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldStack defenderStack = TestHelpers.SimpleStack(
            new WorldStackId("defender"),
            defender.Id,
            new WorldPosition(7, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 2);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Undead, WorldRace.Neutral],
            [WorldRace.Undead],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(defenderStack.Id))],
            [new ScenarioEventWinEffect(defender.Id)],
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
            .WithStack(attackerStack)
            .WithStack(defenderStack)
            .WithCapital(capital)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(attackerStack.Id, capital.Entrance), [
            new WorldStackMovingEvent(attackerStack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("attacker"), new WorldFortificationId("capital")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldStackDestroyedEvent(defenderStack.Id, Destroyer: null, DestroyerPlayerId: null),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, attacker.Id, new ScenarioEventStackDestroyedTrigger(null)),
            new WorldScenarioEndedEvent(defender.Id),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, attacker.Id, new ScenarioEventStackDestroyedTrigger(null)),
            new WorldScenarioEndedEvent(attacker.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldStackMovedEvent(attackerStack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(attackerStack.Id, attackerStack with
                {
                    Position = capital.Entrance,
                    Direction = WorldStackDirection.NorthWest,
                    MovementPoints = 2
                })
                .Remove(defenderStack.Id),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with { CapturedById = attacker.Id }),
            ScenarioState = WorldScenarioState.Empty with
            {
                EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                DestroyedStacks = [new ScenarioDestroyedStack(defenderStack.Id, DestroyerPlayerId: null, SourceTemplateId: null)],
                IsEnded = true,
                Winner = defender.Id
            }
        }));
    }
}
