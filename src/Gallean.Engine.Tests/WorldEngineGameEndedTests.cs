using System.Collections.Immutable;
using Gallean.Common;

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
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map);
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
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
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
            new WorldTurnStartingEvent(otherPlayer.Id),
            new WorldTurnStartedEvent(otherPlayer.Id),
            new WorldTurnResourcesGeneratedEvent(otherPlayer.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
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
            TurnInfo = new WorldTurnInfo(1, otherPlayer.Id),
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
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map) with { IsAi = true };
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
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
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
            new WorldResourcesCapturedEvent(attacker.Id, defender.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
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
            Players = engine.State.Players
                .SetItem(attacker.Id, attacker with { Resources = new WorldResourceSet(200, 25, 0, 0, 25, 0) })
                .SetItem(defender.Id, defender),
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
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map) with { IsAi = true };
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
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
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
            new WorldResourcesCapturedEvent(attacker.Id, defender.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Players = engine.State.Players
                .SetItem(attacker.Id, attacker with { Resources = new WorldResourceSet(200, 25, 0, 0, 25, 0) })
                .SetItem(defender.Id, defender),
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = capital.Entrance,
                Direction = WorldStackDirection.NorthWest,
                Units = new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Experience = 20 }, GridPosition.FrontBottom, false)
                    .Build(),
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
    public void DefeatStacksTest()
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
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Neutral, map) with { IsAi = true };
        WorldSubrace neutralSubrace = new(new WorldSubraceId("neutral"), neutral.Id, WorldSubraceType.Neutral, new WorldObjectVariant(4));
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
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithSubrace(neutralSubrace)
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
            new WorldStackOwnerChangedEvent(defenderStack.Id, neutral.Id, neutralSubrace.Id, PlayAnimation: false),
            new WorldStackOrderChangedEvent(defenderStack.Id, WorldStackOrder.Berserk.Instance),
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
                .SetItem(defenderStack.Id, defenderStack with
                {
                    OwnerId = neutral.Id,
                    SubraceId = neutralSubrace.Id,
                    Order = WorldStackOrder.Berserk.Instance
                }),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with { CapturedById = attacker.Id }),
            ScenarioState = WorldScenarioState.Empty with
            {
                IsEnded = true,
                Winner = attacker.Id
            }
        }));
    }

    [Test]
    public void DefeatDestroysRodsTest()
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
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);
        WorldRod attackerRod = new(new WorldRodId("attacker-rod"), attacker.Id, new WorldPosition(6, 4));
        WorldRod defenderRod = new(new WorldRodId("defender-rod"), defender.Id, new WorldPosition(7, 4));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(attackerStack)
            .WithCapital(capital)
            .WithRod(attackerRod)
            .WithRod(defenderRod)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(attackerStack.Id, capital.Entrance), [
            new WorldStackMovingEvent(attackerStack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("attacker"), new WorldFortificationId("capital")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldRodDestroyedEvent(defenderRod.Id, PlayerId: null, defender.Id, new WorldPosition(7, 4)),
            new WorldScenarioEndedEvent(attacker.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldStackMovedEvent(attackerStack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(attackerStack.Id, attackerStack with
            {
                Position = capital.Entrance,
                Direction = WorldStackDirection.NorthWest,
                MovementPoints = 2
            }),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with { CapturedById = attacker.Id }),
            Rods = engine.State.Rods.Clear().Add(attackerRod.Id, attackerRod),
            ScenarioState = WorldScenarioState.Empty with
            {
                IsEnded = true,
                Winner = attacker.Id
            }
        }));
    }

    [Test]
    public void DefeatGuardBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 6);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Neutral, map) with { IsAi = true };
        WorldSubrace neutralSubrace = new(new WorldSubraceId("neutral"), neutral.Id, WorldSubraceType.Neutral, new WorldObjectVariant(4));
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
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithSubrace(neutralSubrace)
            .WithStack(attackerStack)
            .WithStack(guardStack)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleUnit attackerUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: 100);

        BattleUnit defenderUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("leader2"), leader.Id, BattleSide.Defender, hitpoints: 100);

        BattleState expectedBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(attackerUnit, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defenderUnit, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None,
            Info: new BattleInfo(new BattleSideInfo(IsNeutral: false), new BattleSideInfo(IsNeutral: true)));

        engine.CheckCommand(new WorldMoveStackCommand(attackerStack.Id, capital.Entrance), [
            new WorldStackMovingEvent(attackerStack.Id, new WorldPosition(5, 3), capital.Entrance, WorldStackDirection.West, 2, [], guardStack.Position),
            new WorldFortificationEnteredEvent(attackerStack.Id, capital.Id),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldStackOwnerChangedEvent(guardStack.Id, neutral.Id, neutralSubrace.Id, PlayAnimation: false),
            new WorldStackOrderChangedEvent(guardStack.Id, WorldStackOrder.Berserk.Instance),
            new WorldScenarioEndedEvent(attacker.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldStackMovedEvent(attackerStack.Id, new WorldPosition(5, 3), capital.Entrance, WorldStackDirection.West, 2, [], guardStack.Position),
            new WorldStackMovementConsumedEvent(attackerStack.Id, 1),
            new WorldStackBattleStartedEvent(attackerStack.Id, guardStack.Id, guardStack.Position, expectedBattleState, IsDuel: false)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(attackerStack.Id, attackerStack with
                {
                    Position = capital.Entrance,
                    Direction = WorldStackDirection.West,
                    MovementPoints = 1
                })
                .SetItem(guardStack.Id, guardStack with
                {
                    OwnerId = neutral.Id,
                    SubraceId = neutralSubrace.Id,
                    Order = WorldStackOrder.Berserk.Instance
                }),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with { CapturedById = attacker.Id }),
            ScenarioState = WorldScenarioState.Empty with
            {
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
    public void ScriptedWinPlayerDefeatTest()
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
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Neutral, map) with { IsAi = true };
        WorldSubrace neutralSubrace = new(new WorldSubraceId("neutral"), neutral.Id, WorldSubraceType.Neutral, new WorldObjectVariant(4));
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
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithSubrace(neutralSubrace)
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
            new WorldStackOwnerChangedEvent(defenderStack.Id, neutral.Id, neutralSubrace.Id, PlayAnimation: false),
            new WorldStackOrderChangedEvent(defenderStack.Id, WorldStackOrder.Berserk.Instance),
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
                .SetItem(defenderStack.Id, defenderStack with
                {
                    OwnerId = neutral.Id,
                    SubraceId = neutralSubrace.Id,
                    Order = WorldStackOrder.Berserk.Instance
                }),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with { CapturedById = attacker.Id }),
            ScenarioState = WorldScenarioState.Empty
        }));
    }

    [Test]
    public void WinEventTest()
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
            [new ScenarioEventLootingRuinCondition(new WorldFortificationId("ruin"))],
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
            ScenarioState = WorldScenarioState.Empty
        }));
    }

    [Test]
    public void DefeatCityStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(12, 8);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Neutral, map) with { IsAi = true };
        WorldSubrace neutralSubrace = new(new WorldSubraceId("neutral"), neutral.Id, WorldSubraceType.Neutral, new WorldObjectVariant(4));
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Heretic, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            defender.Id,
            new WorldPosition(9, 6),
            UnitGrid<WorldUnit>.Empty);
        WorldStack attackerStack = TestHelpers.SimpleStack(
            new WorldStackId("attacker"),
            attacker.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldStack cityStack = TestHelpers.SimpleStack(
            new WorldStackId("city"),
            defender.Id,
            city.Entrance,
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithSubrace(neutralSubrace)
            .WithStack(attackerStack)
            .WithStack(cityStack)
            .WithCapital(capital)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(attackerStack.Id, capital.Entrance), [
            new WorldStackMovingEvent(attackerStack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(attackerStack.Id, capital.Id),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldStackOwnerChangedEvent(cityStack.Id, neutral.Id, neutralSubrace.Id, PlayAnimation: false),
            new WorldStackRelocatedEvent(cityStack.Id, new WorldPosition(9, 6), new WorldPosition(10, 7), WorldStackDirection.South),
            new WorldFortificationExitedEvent(cityStack.Id, city.Id),
            new WorldStackOrderChangedEvent(cityStack.Id, WorldStackOrder.Berserk.Instance),
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
                .SetItem(cityStack.Id, cityStack with
                {
                    OwnerId = neutral.Id,
                    SubraceId = neutralSubrace.Id,
                    Position = new WorldPosition(10, 7),
                    Order = WorldStackOrder.Berserk.Instance
                }),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with { CapturedById = attacker.Id }),
            ScenarioState = WorldScenarioState.Empty with
            {
                IsEnded = true,
                Winner = attacker.Id
            }
        }));
    }
}
