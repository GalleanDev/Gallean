using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineCapitalTests
{
    [Test]
    public void CapitalTurnStartTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^...............",
            "^^^^...............",
            "^^^^...............",
            "^^^^...............",
            "^^^^.......U......."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 200, initiative: 50, damage: 5),
            movementPoints: 10, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height)
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(11, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Hitpoints = 50 }, GridPosition.FrontBottom, unitDefinition.IsLarge)
                .Build(),
            movementPoints: 0);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            stack.Position,
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithStack(stack)
            .WithCapital(capital)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 10),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "^^^^...............",
            "^^^^...............",
            "^^^^...............",
            "^^^^...............",
            "^^^^.......U......."
        ]));
        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player.Id].FogOfWar), Is.EqualTo([
            "?.................?",
            "?.................?",
            "?.................?",
            "?.................?",
            "?.................?"
        ]));
        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Hitpoints = 130 }, GridPosition.FrontBottom, unitDefinition.IsLarge)
                        .Build(),
                    MovementPoints = 10
                })
            }));

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 10),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "^^^^.....U.........",
            "^^^^....UUU........",
            "^^^^....U.U........",
            "^^^^....UUU........",
            "^^^^.....U.U......."
        ]));
    }

    [Test]
    public void MoveOntoEnemyCapitalTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^...",
            "^^^^...",
            "^^^^..U"
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 10, initiative: 50, damage: 5),
                movementPoints: 10, leadership: 6) with
            {
                VisionRadius = 3
            };

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(6, 6),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition), GridPosition.FrontBottom, unitDefinition.IsLarge)
                .Build(),
            movementPoints: 10);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit2"), unitDefinition), GridPosition.FrontBottom, unitDefinition.IsLarge)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Attacker, hitpoints: 10), GridPosition.FrontBottom)
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit2"), unitDefinition.Id, BattleSide.Defender, hitpoints: 10) with { StatusEffects = [new BattleFortificationStatusEffect(50)] }, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, capital.Entrance),
            [
                new WorldStackMovingEvent(stack.Id, new WorldPosition(6, 6), new WorldPosition(5, 5), WorldStackDirection.North, 8, [], capital.Entrance),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(6, 6), new WorldPosition(5, 5), WorldStackDirection.North, 8, [], capital.Entrance),
                new WorldStackMovementConsumedEvent(stack.Id, 3),
                new WorldFortificationBattleStartedEvent(stack.Id, capital.Id, capital.Entrance, expectedBattleState)
            ]);

        engine.CheckCommandError(
            new WorldResolveFortificationBattleCommand(stack.Id, capital.Id, expectedBattleState),
            WorldCommandError.BattleNotFinished);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(5, 5),
                    Direction = WorldStackDirection.North,
                    MovementPoints = 3
                })
            }));
    }

    [Test]
    public void ResolveCapitalBattleTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^...",
            "^^...",
            "^^..U"
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(2, 2),
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
            .WithStack(stack)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: 10) with { Experience = 10, ExperienceGained = 2 }, GridPosition.FrontBottom)
                .Build(BattleModifiers.None)
            with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };
        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 10, Experience = 12 }, GridPosition.FrontBottom, leader.IsLarge)
            .Build();

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(stack.Id, capital.Id, finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    stack.Id,
                    capital.Id,
                    BattleSide.Attacker,
                    expectedAttackerUnits,
                    UnitGrid<WorldUnit>.Empty),
                new WorldStackMovingEvent(stack.Id, new WorldPosition(2, 2), capital.Entrance, WorldStackDirection.South, 2, [], null),
                new WorldFortificationExitedEvent(new WorldStackId("stack"), new WorldFortificationId("capital")),
                new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("capital")),
                new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
                new WorldPlayerDefeatedEvent(defender.Id),
                new WorldScenarioEndedEvent(attacker.Id),
                new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
                new WorldStackMovedEvent(stack.Id, new WorldPosition(2, 2), capital.Entrance, WorldStackDirection.South, 2, [], null)
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = expectedAttackerUnits,
                    Position = capital.Entrance,
                    Direction = WorldStackDirection.South,
                    BattlesWon = 1
                }),
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
                {
                    CapturedById = attacker.Id
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    IsEnded = true,
                    Winner = attacker.Id
                }
            }));
        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "^^^^^",
            "^^^^^",
            "^^...",
            "^^...",
            "^^..U"
        ]));
    }

    [Test]
    public void ResolveCapitalBattleLossTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^...",
            "^^...",
            "^^..U"
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 10, initiative: 50, damage: 5);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Defender, hitpoints: 10) with { Experience = 10, ExperienceGained = 2 }, GridPosition.FrontBottom)
                .Build(BattleModifiers.None)
            with
            {
                Outcome = new BattleOutcome(BattleSide.Defender)
            };
        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Experience = 12 }, GridPosition.FrontBottom, unitDefinition.IsLarge)
            .Build();

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(stack.Id, capital.Id, finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    stack.Id,
                    capital.Id,
                    BattleSide.Defender,
                    UnitGrid<WorldUnit>.Empty,
                    expectedDefenderUnits),
                new WorldStackDestroyedEvent(stack.Id, new WorldUnitGroup.Fortification(capital.Id), defender.Id)
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.Remove(stack.Id),
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
                {
                    Garrison = expectedDefenderUnits
                })
            }));
    }

    [Test]
    public void CaptureEmptyCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 5);

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

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, capital.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("capital")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldScenarioEndedEvent(attacker.Id),
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
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with { CapturedById = attacker.Id }),
            ScenarioState = WorldScenarioState.Empty with
            {
                IsEnded = true,
                Winner = attacker.Id
            }
        }));
        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "......",
            "......",
            "......",
            "......",
            "......"
        ]));
    }

    [Test]
    public void MoveOntoAdjacentDefendedCapitalTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^.."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 2, leadership: 6);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition), GridPosition.FrontBottom, unitDefinition.IsLarge)
                .Build(),
            movementPoints: 2);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit2"), unitDefinition), GridPosition.FrontBottom, unitDefinition.IsLarge)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Attacker, hitpoints: 10), GridPosition.FrontBottom)
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit2"), unitDefinition.Id, BattleSide.Defender, hitpoints: 10) with { StatusEffects = [new BattleFortificationStatusEffect(50)] }, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, capital.Entrance), [
            new WorldStackMovementConsumedEvent(stack.Id, 1),
            new WorldFortificationBattleStartedEvent(stack.Id, capital.Id, capital.Entrance, expectedBattleState)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Direction = WorldStackDirection.NorthWest,
                    MovementPoints = 1
                })
            }));
    }

    [Test]
    public void CaptureCapitalSpellsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 5);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [new WorldSpellDefinitionId("fire"), new WorldSpellDefinitionId("frost")])
        };

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
            .WithStack(stack)
            .WithCapital(capital)
            .WithRandom(new TestSequenceRandomProvider(10))
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, capital.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("capital")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldSpellsReceivedEvent(attacker.Id, [new WorldSpellDefinitionId("fire"), new WorldSpellDefinitionId("frost")]),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldScenarioEndedEvent(attacker.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Players = engine.State.Players.SetItem(attacker.Id, attacker with
            {
                Resources = new WorldResourceSet(100, 25, 0, 0, 0, 0),
                Spellbook = new WorldPlayerSpellbook([], [new WorldSpellDefinitionId("fire"), new WorldSpellDefinitionId("frost")])
            }),
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = capital.Entrance,
                Direction = WorldStackDirection.NorthWest,
                MovementPoints = 2
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
    public void FailedSpellRollTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 5);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [new WorldSpellDefinitionId("fire"), new WorldSpellDefinitionId("frost")])
        };

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
            .WithStack(stack)
            .WithCapital(capital)
            .WithRandom(new TestSequenceRandomProvider(90))
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, capital.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("capital")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldScenarioEndedEvent(attacker.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Players = engine.State.Players.SetItem(attacker.Id, attacker with
            {
                Resources = new WorldResourceSet(100, 25, 0, 0, 0, 0)
            }),
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = capital.Entrance,
                Direction = WorldStackDirection.NorthWest,
                MovementPoints = 2
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
    public void CapturedCapitalTest()
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
        _ = engine.DispatchCommand(new WorldMoveStackCommand(stack.Id, capturedCapital.Entrance));

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(5, 4)), [
            new WorldStackMovingEvent(stack.Id, capturedCapital.Entrance, new WorldPosition(5, 4), WorldStackDirection.SouthEast, 2, [], null),
            new WorldFortificationExitedEvent(new WorldStackId("stack"), new WorldFortificationId("capital1")),
            new WorldStackMovedEvent(stack.Id, capturedCapital.Entrance, new WorldPosition(5, 4), WorldStackDirection.SouthEast, 2, [], null)
        ]);

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, capturedCapital.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 4), capturedCapital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("capital1")),
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
            Fortifications = engine.State.Fortifications.SetItem(capturedCapital.Id, capturedCapital with { CapturedById = attacker.Id }),
            ScenarioState = WorldScenarioState.Empty
        }));
    }

    [Test]
    public void CaptureCapitalInventoryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 5);

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
                movementPoints: 2) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Potion"), new ItemDefinitionId("potion"), Charges: null)])
            };
        WorldCapital capital = TestHelpers.SimpleCapital(
                new WorldFortificationId("capital"),
                defender.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([
                    new WorldItem(new WorldItemId("Relic"), new ItemDefinitionId("relic"), Charges: null),
                    new WorldItem(new WorldItemId("Talisman"), new ItemDefinitionId("talisman"), Charges: 3)
                ])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, capital.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("capital")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldScenarioEndedEvent(attacker.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = capital.Entrance,
                Direction = WorldStackDirection.NorthWest,
                MovementPoints = 2,
                Inventory = new WorldInventory([
                    new WorldItem(new WorldItemId("Potion"), new ItemDefinitionId("potion"), Charges: null),
                    new WorldItem(new WorldItemId("Relic"), new ItemDefinitionId("relic"), Charges: null),
                    new WorldItem(new WorldItemId("Talisman"), new ItemDefinitionId("talisman"), Charges: 3)
                ])
            }),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
            {
                Inventory = WorldInventory.Empty,
                CapturedById = attacker.Id
            }),
            ScenarioState = WorldScenarioState.Empty with
            {
                IsEnded = true,
                Winner = attacker.Id
            }
        }));
    }
}
