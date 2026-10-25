namespace Gallean.Engine.Tests;

public sealed class WorldEngineCityTests
{
    [Test]
    public void MoveOntoEnemyCityTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^...",
            "^^^...",
            "^^^..U"
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Unit1", maxHitpoints: 10, initiative: 50, damage: 5),
                movementPoints: 10, leadership: 6) with
            {
                VisionRadius = 3
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            new WorldPlayerId(1),
            new WorldPosition(5, 5),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition), new GridPosition(0, 0), unitDefinition.IsLarge)
                .Build(),
            movementPoints: 10);

        UnitModifierDefinition protection = TestHelpers.SimpleModifier("protection") with { Armor = new UnitStatModifier(10, 0) };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty) with
        {
            ProtectionModifierId = protection.Id
        };
        WorldCity city1 = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            new WorldPlayerId(2),
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit2"), unitDefinition), new GridPosition(0, 0), unitDefinition.IsLarge)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(protection)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithCity(city1)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(),
        [
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldStackMovementRestoredEvent(new WorldStackId("stack1"), 10),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).AddUnitModifier(protection).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Attacker, hitpoints: 10), new GridPosition(0, 0))
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit2"), unitDefinition.Id, BattleSide.Defender, hitpoints: 10) with { Modifiers = [UnitModifier.FromFortification(protection.Id)], StatusEffects = [new BattleFortificationStatusEffect(10)] }, new GridPosition(0, 0))
            .Build(BattleModifiers.None);

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("stack1"), new WorldPosition(3, 3)),
            [
                new WorldStackMovingEvent(new WorldStackId("stack1"), new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 8, [], new WorldPosition(3, 3)),
                new WorldStackMovedEvent(new WorldStackId("stack1"), new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 8, [], new WorldPosition(3, 3)),
                new WorldStackMovementConsumedEvent(new WorldStackId("stack1"), 3),
                new WorldFortificationBattleStartedEvent(new WorldStackId("stack1"), new WorldFortificationId("city1"), new WorldPosition(3, 3), expectedBattleState)
            ]);

        engine.CheckCommandError(
            new WorldResolveFortificationBattleCommand(new WorldStackId("stack1"), new WorldFortificationId("city1"), expectedBattleState),
            WorldCommandError.BattleNotFinished);

        WorldStack expectedStack = stack1 with
        {
            Position = new WorldPosition(4, 4),
            Direction = WorldStackDirection.North,
            MovementPoints = 3
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(expectedStack.Id, expectedStack)
            }));
    }

    [Test]
    public void ResolveCityBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7, 'R');

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);
        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city1 = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            new WorldPlayerId(2),
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithCity(city1)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(),
        [
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldStackMovementRestoredEvent(new WorldStackId("Stack1"), 10),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: 10) with { Experience = 3, ExperienceGained = 4 }, new GridPosition(0, 0))
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 10, Experience = 7 }, new GridPosition(0, 0), leader.IsLarge)
            .Build();

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(new WorldStackId("Stack1"), new WorldFortificationId("City1"), finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    new WorldStackId("Stack1"),
                    new WorldFortificationId("City1"),
                    BattleSide.Attacker,
                    expectedAttackerUnits,
                    UnitGrid<WorldUnit>.Empty),
                new WorldStackMovingEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(3, 3), WorldStackDirection.South, 10, [], null),
                new WorldFortificationExitedEvent(new WorldStackId("Stack1"), new WorldFortificationId("City1")),
                new WorldFortificationEnteredEvent(new WorldStackId("Stack1"), new WorldFortificationId("City1")),
                new WorldFortificationCapturingEvent(
                    new WorldFortificationId("City1"),
                    new WorldPlayerId(2),
                    new WorldPlayerId(1),
                    new WorldStackId("Stack1")),
                new WorldFortificationCapturedEvent(
                    new WorldFortificationId("City1"),
                    new WorldPlayerId(2),
                    new WorldPlayerId(1),
                    new WorldStackId("Stack1")),
                new WorldStackMovedEvent(new WorldStackId("Stack1"), new WorldPosition(0, 0), new WorldPosition(3, 3), WorldStackDirection.South, 10, [], null)
            ]
        );

        WorldPlayer expectedPlayer1 = player1 with
        {
            Resources = new WorldResourceSet(100, 25, 0, 0, 0, 0)
        };
        WorldStack expectedStack = stack1 with
        {
            Units = expectedAttackerUnits,
            Position = new WorldPosition(3, 3),
            Direction = WorldStackDirection.South,
            BattlesWon = 1
        };
        WorldCity expectedCity = city1 with
        {
            OwnerId = new WorldPlayerId(1)
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(expectedPlayer1.Id, expectedPlayer1),
                Stacks = engine.State.Stacks.SetItem(expectedStack.Id, expectedStack),
                Fortifications = engine.State.Fortifications.SetItem(expectedCity.Id, expectedCity)
            }));
        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "RRURRRR",
            "RUUURRR",
            "UURUURR",
            "RUUURRR",
            "RUUURRR",
            "RRURRRR",
            "RRRRRRR"
        ]));
    }

    [Test]
    public void ResolveCityBattleLossTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7, 'R');

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);
        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city1 = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            new WorldPlayerId(2),
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(unitDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithCity(city1)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Defender, hitpoints: 10) with { Experience = 3, ExperienceGained = 4 }, new GridPosition(0, 0))
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Experience = 7 }, new GridPosition(0, 0), unitDefinition.IsLarge)
            .Build();

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(new WorldStackId("Stack1"), new WorldFortificationId("City1"), finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    new WorldStackId("Stack1"),
                    new WorldFortificationId("City1"),
                    BattleSide.Defender,
                    UnitGrid<WorldUnit>.Empty,
                    expectedDefenderUnits),
                new WorldStackDestroyedEvent(new WorldStackId("Stack1"), new WorldUnitGroup.Fortification(city1.Id), city1.OwnerId)
            ]
        );

        WorldCity expectedCity = city1 with
        {
            Garrison = expectedDefenderUnits
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.Remove(new WorldStackId("Stack1")),
                Fortifications = engine.State.Fortifications.SetItem(expectedCity.Id, expectedCity)
            }));
    }

    [Test]
    public void CaptureEmptyCityTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".....",
            ".....",
            ".....",
            "^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            defender.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

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
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, city.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(4, 3), city.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("city")),
            new WorldFortificationCapturingEvent(city.Id, defender.Id, attacker.Id, stack.Id),
            new WorldFortificationCapturedEvent(city.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(4, 3), city.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = city.Entrance,
                Direction = WorldStackDirection.NorthWest,
                MovementPoints = 2
            }),
            Fortifications = engine.State.Fortifications.SetItem(city.Id, city with { OwnerId = attacker.Id })
        }));
        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            ".UUUU",
            "UUUUU",
            "UU.UU",
            "^^^UU"
        ]));
    }

    [Test]
    public void RegenerateInCityTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^U.."
        ]);

        UnitDefinition unitDef1 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("unit1", maxHitpoints: 100, initiative: 50, damage: 5),
            movementPoints: 5, leadership: 6);

        UnitDefinition unitDef2 = TestHelpers.SimpleUnitDefinition("unit2", maxHitpoints: 100, initiative: 50, damage: 5);

        UnitDefinition unitDef3 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("unit3", maxHitpoints: 100, initiative: 50, damage: 5),
            movementPoints: 5, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city1 = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            new WorldPlayerId(1),
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            new WorldPlayerId(1),
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDef1"), unitDef1) with { Hitpoints = 50, Experience = 7 }, new GridPosition(0, 0), unitDef1.IsLarge)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDef2"), unitDef2) with { Hitpoints = 90, Experience = 8 }, new GridPosition(1, 0), unitDef2.IsLarge)
                .Build(),
            movementPoints: 5);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("stack2"),
            new WorldPlayerId(1),
            new WorldPosition(5, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDef3"), unitDef3) with { Hitpoints = 50, Experience = 9 }, new GridPosition(0, 0), unitDef3.IsLarge)
                .Build(),
            movementPoints: 5);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDef1)
            .WithDefinition(unitDef2)
            .WithDefinition(unitDef3)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Mage))
            .WithPlayer(player2)
            .WithCity(city1)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(),
        [
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldStackMovementRestoredEvent(new WorldStackId("stack1"), 5),
            new WorldStackMovementRestoredEvent(new WorldStackId("stack2"), 5),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(),
        [
            new WorldTurnEndedEvent(new WorldPlayerId(1)),
            new WorldTurnStartingEvent(new WorldPlayerId(2)),
            new WorldTurnStartedEvent(new WorldPlayerId(2)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(2), new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(),
        [
            new WorldTurnEndedEvent(new WorldPlayerId(2)),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(new WorldPlayerId(1)),
            new WorldStackMovementRestoredEvent(new WorldStackId("stack1"), 5),
            new WorldStackMovementRestoredEvent(new WorldStackId("stack2"), 5),
            new WorldTurnStartedEvent(new WorldPlayerId(1)),
            new WorldTurnResourcesGeneratedEvent(new WorldPlayerId(1), new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        WorldPlayer expectedPlayer1 = player1 with
        {
            Resources = new WorldResourceSet(300, 50, 0, 0, 0, 0)
        };
        WorldPlayer expectedPlayer2 = player2 with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 25, 0)
        };
        WorldStack expectedStack1 = stack1 with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDef1"), unitDef1) with { Hitpoints = 80, Experience = 7 }, new GridPosition(0, 0), unitDef1.IsLarge)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDef2"), unitDef2) with { Experience = 8 }, new GridPosition(1, 0), unitDef2.IsLarge)
                .Build()
        };
        WorldStack expectedStack2 = stack2 with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDef3"), unitDef3) with { Hitpoints = 60, Experience = 9 }, new GridPosition(0, 0), unitDef3.IsLarge)
                .Build()
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players
                    .SetItem(expectedPlayer1.Id, expectedPlayer1)
                    .SetItem(expectedPlayer2.Id, expectedPlayer2),
                Stacks = engine.State.Stacks
                    .SetItem(expectedStack1.Id, expectedStack1)
                    .SetItem(expectedStack2.Id, expectedStack2)
            }));
    }

    [Test]
    public void RegenerateInCityDeadUnitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^U.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 5),
            movementPoints: 5, leadership: 6);

        UnitDefinition woundedDefinition = TestHelpers.SimpleUnitDefinition("wounded", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition deadDefinition = TestHelpers.SimpleUnitDefinition("dead", maxHitpoints: 100, initiative: 50, damage: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(1, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("wounded"), woundedDefinition) with { Hitpoints = 50 }, new GridPosition(0, 0), false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("dead"), deadDefinition) with { Hitpoints = 0 }, new GridPosition(0, 1), false)
                .Build(),
            movementPoints: 5);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(woundedDefinition)
            .WithDefinition(deadDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), new GridPosition(1, 0), false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("wounded"), woundedDefinition) with { Hitpoints = 65 }, new GridPosition(0, 0), false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("dead"), deadDefinition) with { Hitpoints = 0 }, new GridPosition(0, 1), false)
                        .Build()
                })
            }));
    }

    [Test]
    public void ThiefCaptureForbiddenTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition() with
        {
            Leadership = 5,
            MovementPoints = 2
        };
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 2);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            defender.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            WorldCommandError.FortificationCaptureForbidden);
    }

    [Test]
    public void SummonCaptureForbiddenSentryTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^..",
            "^^..."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition summon = TestHelpers.SimpleUnitDefinition("summon", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Summon,
            Leadership = 1,
            MovementPoints = 2,
            Lifetime = 5
        };
        UnitDefinition sentry = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("sentry", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                attacker.Id,
                new WorldPosition(4, 3),
                TestHelpers.SingleUnitGrid(new WorldUnitId("summon"), summon),
                movementPoints: 2) with
            {
                ExpirationTurn = 5
            };
        WorldStack sentryStack = TestHelpers.SimpleStack(
            new WorldStackId("sentry"),
            defender.Id,
            new WorldPosition(2, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("sentry"), sentry),
            movementPoints: 2);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            defender.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(sentry)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithStack(sentryStack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            WorldCommandError.FortificationCaptureForbidden);
    }

    [Test]
    public void SummonCaptureForbiddenTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition summon = TestHelpers.SimpleUnitDefinition("summon", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Summon,
            Leadership = 1,
            MovementPoints = 2,
            Lifetime = 5
        };
        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                attacker.Id,
                new WorldPosition(4, 3),
                TestHelpers.SingleUnitGrid(new WorldUnitId("summon"), summon),
                movementPoints: 2) with
            {
                ExpirationTurn = 5
            };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            defender.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            WorldCommandError.FortificationCaptureForbidden);
    }

    [Test]
    public void ThiefCaptureAfterBattleForbiddenTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition() with
        {
            Leadership = 5,
            MovementPoints = 2
        };
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 2);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            defender.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(thief).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("thief"), thief.Id, BattleSide.Attacker, hitpoints: 40) with { Experience = 3, ExperienceGained = 4 }, GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief) with { Hitpoints = 40, Experience = 7 }, GridPosition.FrontBottom, thief.IsLarge)
            .Build();

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(stack.Id, city.Id, finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    stack.Id,
                    city.Id,
                    BattleSide.Attacker,
                    expectedAttackerUnits,
                    UnitGrid<WorldUnit>.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with { Units = expectedAttackerUnits, BattlesWon = 1 }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city)
            }));
        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^.."
        ]));
    }

    [Test]
    public void ThiefCaptureForbiddenSentryTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^..",
            "....."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition() with
        {
            Leadership = 5,
            MovementPoints = 2
        };
        UnitDefinition sentryLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("sentry", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 2);
        WorldStack sentry = TestHelpers.SimpleStack(
            new WorldStackId("sentry"),
            defender.Id,
            new WorldPosition(2, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("sentryLeader"), sentryLeader),
            movementPoints: 2);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            defender.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(sentryLeader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithStack(sentry)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldMoveStackCommand(stack.Id, city.Entrance),
            WorldCommandError.FortificationCaptureForbidden);
    }

    [Test]
    public void CaptureCityInventoryTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".....",
            ".....",
            ".....",
            "^^^.."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                defender.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Relic"), new ItemDefinitionId("relic"), Charges: null)])
            };

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
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, city.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(4, 3), city.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("city")),
            new WorldFortificationCapturingEvent(city.Id, defender.Id, attacker.Id, stack.Id),
            new WorldFortificationCapturedEvent(city.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(4, 3), city.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = city.Entrance,
                Direction = WorldStackDirection.NorthWest,
                MovementPoints = 2
            }),
            Fortifications = engine.State.Fortifications.SetItem(city.Id, city with { OwnerId = attacker.Id })
        }));
    }

    [Test]
    public void CaptureCityLevelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 4);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            attacker.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 3), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            defender.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(new GlobalVariables { CityCaptureTerrainPercent = 250, CityTerrainTilesPerTurnLevel3 = 3 })
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(stack.Id, city.Entrance), [
            new WorldStackMovingEvent(stack.Id, new WorldPosition(4, 3), city.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("stack"), new WorldFortificationId("city")),
            new WorldFortificationCapturingEvent(city.Id, defender.Id, attacker.Id, stack.Id),
            new WorldFortificationCapturedEvent(city.Id, defender.Id, attacker.Id, stack.Id),
            new WorldStackMovedEvent(stack.Id, new WorldPosition(4, 3), city.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
            {
                Position = city.Entrance,
                Direction = WorldStackDirection.NorthWest,
                MovementPoints = 2
            }),
            Fortifications = engine.State.Fortifications.SetItem(city.Id, city with { OwnerId = attacker.Id })
        }));
        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            ".....",
            "..U..",
            ".U.U.",
            ".UUU."
        ]));
    }

    [Test]
    public void CaptureCitySubraceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 4);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                attacker.Id,
                new WorldPosition(4, 3),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 2) with
            {
                SubraceId = new WorldSubraceId("attacker-subrace")
            };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            defender.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

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
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldMoveStackCommand(stack.Id, city.Entrance));

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with
        {
            OwnerId = attacker.Id,
            SubraceId = new WorldSubraceId("attacker-subrace")
        }));
    }

    [Test]
    public void AttackVisitingStackTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^...",
            "^^^...",
            "^^^..U"
        ]);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack attacker = TestHelpers.SimpleStack(
            new WorldStackId("attacker"),
            player1.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("attacker-leader"), leader),
            movementPoints: 10);

        WorldStack visitor = TestHelpers.SimpleStack(
            new WorldStackId("visitor"),
            player2.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("visitor-leader"), leader),
            movementPoints: 10);

        UnitModifierDefinition protection = TestHelpers.SimpleModifier("protection") with { Armor = new UnitStatModifier(15, 0) };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 2), WorldResourceSet.Empty) with
        {
            ProtectionModifierId = protection.Id
        };
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("garrison-leader"), leader));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(protection)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(attacker)
            .WithStack(visitor)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).AddUnitModifier(protection).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("attacker-leader"), leader.Id, BattleSide.Attacker, hitpoints: 100), GridPosition.FrontBottom)
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("visitor-leader"), leader.Id, BattleSide.Defender, hitpoints: 100) with { Modifiers = [UnitModifier.FromFortification(protection.Id)], StatusEffects = [new BattleFortificationStatusEffect(15)] }, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        engine.CheckCommand(
            new WorldMoveStackCommand(attacker.Id, new WorldPosition(3, 3)),
            [
                new WorldStackMovingEvent(attacker.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 8, [], new WorldPosition(3, 3)),
                new WorldStackMovedEvent(attacker.Id, new WorldPosition(5, 5), new WorldPosition(4, 4), WorldStackDirection.North, 8, [], new WorldPosition(3, 3)),
                new WorldStackMovementConsumedEvent(attacker.Id, 3),
                new WorldStackBattleStartedEvent(attacker.Id, visitor.Id, new WorldPosition(3, 3), expectedBattleState, IsDuel: false)
            ]);
    }
}
