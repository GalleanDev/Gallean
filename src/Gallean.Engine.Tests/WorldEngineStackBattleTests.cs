namespace Gallean.Engine.Tests;

public sealed class WorldEngineStackBattleTests
{
    [Test]
    public void MoveOntoEnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition unitDefinition1 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition unitDefinition2 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack2", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        UnitModifierDefinition modifierDefinition = TestHelpers.SimpleModifier("modifier");

        WorldUnit unit1 = new(new WorldUnitId("unit1"), unitDefinition1.Id, 10, 3, 1, [UnitModifier.Permanent(modifierDefinition.Id)], null, false);
        WorldUnit unit2 = new(new WorldUnitId("unit2"), unitDefinition2.Id, 10, 4, 1, [], null, false);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(unit1, new GridPosition(0, 0), false).Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(unit2, new GridPosition(0, 0), false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition1)
            .WithDefinition(unitDefinition2)
            .WithDefinition(modifierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit battleUnit1 = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("unit1"),
            DefinitionId: unitDefinition1.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 10,
            Experience: 3,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [UnitModifier.Permanent(modifierDefinition.Id)],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit battleUnit2 = new(
            Id: new BattleUnitId(1),
            WorldUnitId: new WorldUnitId("unit2"),
            DefinitionId: unitDefinition2.Id,
            Side: BattleSide.Defender,
            Hitpoints: 10,
            Experience: 4,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleState expectedBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(battleUnit1, new GridPosition(0, 0), false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(battleUnit2, new GridPosition(0, 0), false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None
        );

        engine.CheckCommand(new WorldMoveStackCommand(stack1.Id, stack2.Position), [
            new WorldStackMovingEvent(stack1.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], stack2.Position),
            new WorldStackMovedEvent(stack1.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], stack2.Position),
            new WorldStackMovementConsumedEvent(stack1.Id, 1),
            new WorldStackBattleStartedEvent(stack1.Id, stack2.Id, stack2.Position, expectedBattleState, IsDuel: false)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack1.Id, stack1 with
            {
                Position = new WorldPosition(1, 0),
                Direction = WorldStackDirection.SouthEast,
                MovementPoints = 1
            })
        }));
    }

    [Test]
    public void LeaderNameTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition enemyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, 1, [], Name: "Nigosh", IsTypeLocked: false);
        WorldUnit enemy = new(new WorldUnitId("enemy"), enemyDefinition.Id, 10, 0, 1, [], null, false);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(leader, GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(enemy, GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit battleLeader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
        {
            Name = "Nigosh"
        };

        BattleUnit battleEnemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleState initialBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(battleLeader, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(battleEnemy, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        engine.CheckCommand(new WorldMoveStackCommand(stack1.Id, stack2.Position), [
            new WorldStackMovingEvent(stack1.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], stack2.Position),
            new WorldStackMovedEvent(stack1.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 4, [], stack2.Position),
            new WorldStackMovementConsumedEvent(stack1.Id, 1),
            new WorldStackBattleStartedEvent(stack1.Id, stack2.Id, stack2.Position, initialBattleState, IsDuel: false)
        ]);

        BattleState finalState = initialBattleState with
        {
            DefenderGrid = UnitGrid<BattleUnit>.Empty,
            Outcome = new BattleOutcome(BattleSide.Attacker)
        };

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalState, IsDuel: false), [
            new WorldStackBattleEndedEvent(
                stack1.Id,
                stack2.Id,
                BattleSide.Attacker,
                new UnitGrid<WorldUnit>.Builder().Place(leader, GridPosition.FrontBottom, false).Build(),
                UnitGrid<WorldUnit>.Empty),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with
                {
                    Position = new WorldPosition(1, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 1,
                    BattlesWon = 1
                })
                .Remove(stack2.Id)
        }));
    }

    [Test]
    public void MoveOntoEnemyOneMovementPointTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition unitDefinition1 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 10, leadership: 6);
        UnitDefinition unitDefinition2 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack2", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDefinition1"), unitDefinition1), new GridPosition(0, 0), false).Build(),
            movementPoints: 1);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDefinition2"), unitDefinition2), new GridPosition(0, 0), false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition1)
            .WithDefinition(unitDefinition2)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleState expectedBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unitDefinition1"), unitDefinition1.Id, BattleSide.Attacker, hitpoints: 10), new GridPosition(0, 0), false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unitDefinition2"), unitDefinition2.Id, BattleSide.Defender, hitpoints: 10), new GridPosition(0, 0), false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None
        );

        engine.CheckCommand(new WorldMoveStackCommand(stack1.Id, stack2.Position), [
            new WorldStackMovementConsumedEvent(stack1.Id, 0),
            new WorldStackBattleStartedEvent(stack1.Id, stack2.Id, stack2.Position, expectedBattleState, IsDuel: false)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack1.Id, stack1 with
            {
                Direction = WorldStackDirection.SouthEast,
                MovementPoints = 0
            })
        }));
    }

    [Test]
    public void MoveAdjacentToEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition unitDefinition1 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 10, leadership: 6);
        UnitDefinition unitDefinition2 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack2", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDefinition1"), unitDefinition1), new GridPosition(0, 0), false).Build(),
            movementPoints: 10);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(3, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDefinition2"), unitDefinition2), new GridPosition(0, 0), false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition1)
            .WithDefinition(unitDefinition2)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleState expectedBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unitDefinition1"), unitDefinition1.Id, BattleSide.Attacker, hitpoints: 10), new GridPosition(0, 0), false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unitDefinition2"), unitDefinition2.Id, BattleSide.Defender, hitpoints: 10), new GridPosition(0, 0), false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None
        );

        engine.CheckCommand(new WorldMoveStackCommand(stack1.Id, new WorldPosition(2, 0)), [
            new WorldStackMovingEvent(stack1.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 8, [new WorldPosition(2, 0)], stack2.Position),
            new WorldStackMovedEvent(stack1.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 8, [new WorldPosition(2, 0)], stack2.Position),
            new WorldStackMovingEvent(stack1.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 6, [], stack2.Position),
            new WorldStackMovedEvent(stack1.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 6, [], stack2.Position),
            new WorldStackMovementConsumedEvent(stack1.Id, 1),
            new WorldStackBattleStartedEvent(stack1.Id, stack2.Id, stack2.Position, expectedBattleState, IsDuel: false)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack1.Id, stack1 with
            {
                Position = new WorldPosition(2, 0),
                Direction = WorldStackDirection.SouthEast,
                MovementPoints = 1
            })
        }));
    }

    [Test]
    public void MoveAdjacentToEnemyExactMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition unitDefinition1 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 10, leadership: 6);
        UnitDefinition unitDefinition2 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack2", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDefinition1"), unitDefinition1), new GridPosition(0, 0), false).Build(),
            movementPoints: 4);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(3, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDefinition2"), unitDefinition2), new GridPosition(0, 0), false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition1)
            .WithDefinition(unitDefinition2)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleState expectedBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unitDefinition1"), unitDefinition1.Id, BattleSide.Attacker, hitpoints: 10), new GridPosition(0, 0), false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unitDefinition2"), unitDefinition2.Id, BattleSide.Defender, hitpoints: 10), new GridPosition(0, 0), false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None
        );

        engine.CheckCommand(new WorldMoveStackCommand(stack1.Id, new WorldPosition(2, 0)), [
            new WorldStackMovingEvent(stack1.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 2, [new WorldPosition(2, 0)], stack2.Position),
            new WorldStackMovedEvent(stack1.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 2, [new WorldPosition(2, 0)], stack2.Position),
            new WorldStackMovingEvent(stack1.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 0, [], stack2.Position),
            new WorldStackMovedEvent(stack1.Id, new WorldPosition(1, 0), new WorldPosition(2, 0), WorldStackDirection.SouthEast, 0, [], stack2.Position),
            new WorldStackMovementConsumedEvent(stack1.Id, 0),
            new WorldStackBattleStartedEvent(stack1.Id, stack2.Id, stack2.Position, expectedBattleState, IsDuel: false)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack1.Id, stack1 with
            {
                Position = new WorldPosition(2, 0),
                Direction = WorldStackDirection.SouthEast,
                MovementPoints = 0
            })
        }));
    }

    [Test]
    public void MoveTowardsEnemyOutOfMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition unitDefinition1 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 10, leadership: 6);
        UnitDefinition unitDefinition2 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack2", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDefinition1"), unitDefinition1), new GridPosition(0, 0), false).Build(),
            movementPoints: 2);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(3, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDefinition2"), unitDefinition2), new GridPosition(0, 0), false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition1)
            .WithDefinition(unitDefinition2)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        engine.CheckCommand(new WorldMoveStackCommand(stack1.Id, new WorldPosition(2, 0)), [
            new WorldStackMovingEvent(stack1.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 0, [], null),
            new WorldStackMovedEvent(stack1.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 0, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack1.Id, stack1 with
            {
                Position = new WorldPosition(1, 0),
                Direction = WorldStackDirection.SouthEast,
                MovementPoints = 0
            })
        }));
    }

    [Test]
    public void MovePastEnemyTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...",
            "^^.",
            "..."
        ]);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition unitDefinition1 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 10, leadership: 6);
        UnitDefinition unitDefinition2 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack2", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDefinition1"), unitDefinition1), new GridPosition(0, 0), false).Build(),
            movementPoints: 10);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("unitDefinition2"), unitDefinition2), new GridPosition(0, 0), false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition1)
            .WithDefinition(unitDefinition2)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleState expectedBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unitDefinition1"), unitDefinition1.Id, BattleSide.Attacker, hitpoints: 10), new GridPosition(0, 0), false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unitDefinition2"), unitDefinition2.Id, BattleSide.Defender, hitpoints: 10), new GridPosition(0, 0), false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None
        );

        engine.CheckCommand(new WorldMoveStackCommand(stack1.Id, new WorldPosition(0, 2)), [
            new WorldStackMovingEvent(stack1.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 8, [], stack2.Position),
            new WorldStackMovedEvent(stack1.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 8, [], stack2.Position),
            new WorldStackMovementConsumedEvent(stack1.Id, 3),
            new WorldStackBattleStartedEvent(stack1.Id, stack2.Id, stack2.Position, expectedBattleState, IsDuel: false)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack1.Id, stack1 with
            {
                Position = new WorldPosition(1, 0),
                Direction = WorldStackDirection.SouthEast,
                MovementPoints = 3
            })
        }));
    }

    [Test]
    public void MoveOntoEnemyInsufficientMovementTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^",
            "^^^^^^^^",
            "^^......",
            "^^......",
            "^^......",
            "^^......",
            "^^......",
            "^^......"
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldStack attackerStack = TestHelpers.SimpleStack(
            new WorldStackId("attacker"),
            attacker.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 0);

        WorldStack defenderStack = TestHelpers.SimpleStack(
            new WorldStackId("defender"),
            defender.Id,
            new WorldPosition(6, 6),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 10);

        WorldUnit defenderUnit = new(new WorldUnitId("defenderUnit"), new UnitDefinitionId("unit"), 1, 0, 1, [], null, false);
        UnitGrid<WorldUnit> garrison = new UnitGrid<WorldUnit>.Builder()
            .Place(defenderUnit, GridPosition.FrontBottom, false)
            .Build();

        WorldCity city = new(
            Id: new WorldFortificationId("city"),
            NameTextId: new ScenarioTextId("Name"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Heretic, 1),
            OwnerId: defender.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(5, 4), FortificationCalculator.CityFootprint),
            Garrison: garrison,
            Inventory: WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0
        );

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 5),
            new UnitGrid<WorldUnit>.Builder()
                .Place(defenderUnit with { Id = new WorldUnitId("capitalDefenderUnit") }, GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(attackerStack)
            .WithStack(defenderStack)
            .WithCity(city)
            .WithCapital(capital)
            .Build();

        engine.CheckCommandError(
            new WorldMoveStackCommand(attackerStack.Id, defenderStack.Position),
            WorldCommandError.InsufficientMovement);

        engine.CheckCommandError(
            new WorldMoveStackCommand(attackerStack.Id, city.Entrance),
            WorldCommandError.InsufficientMovement);

        engine.CheckCommandError(
            new WorldMoveStackCommand(attackerStack.Id, capital.Entrance),
            WorldCommandError.InsufficientMovement);
    }

    [Test]
    public void MoveOntoEnemyBeyondMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);

        WorldStack attackerStack = TestHelpers.SimpleStack(
            new WorldStackId("attacker"),
            attacker.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);

        WorldStack defenderStack = TestHelpers.SimpleStack(
            new WorldStackId("defender"),
            defender.Id,
            new WorldPosition(3, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(attackerStack)
            .WithStack(defenderStack)
            .Build();

        engine.CheckCommand(
            new WorldMoveStackCommand(attackerStack.Id, defenderStack.Position),
            [
                new WorldStackMovingEvent(attackerStack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 0, [], null),
                new WorldStackMovedEvent(attackerStack.Id, new WorldPosition(0, 0), new WorldPosition(1, 0), WorldStackDirection.SouthEast, 0, [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(attackerStack.Id, attackerStack with
            {
                Position = new WorldPosition(1, 0),
                Direction = WorldStackDirection.SouthEast,
                MovementPoints = 0
            })
        }));
    }

    [Test]
    public void StartBattleDifficultyBonusTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        UnitDefinition unitDefinition1 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition unitDefinition2 = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Unit_Stack2", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldUnit unit1 = new(new WorldUnitId("unit1"), unitDefinition1.Id, 10, 0, 1, [], null, false);
        WorldUnit unit2 = new(new WorldUnitId("unit2"), unitDefinition2.Id, 10, 0, 1, [], null, false);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(unit1, new GridPosition(0, 0), false).Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(unit2, new GridPosition(0, 0), false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Easy)
            .WithDefinition(unitDefinition1)
            .WithDefinition(unitDefinition2)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit battleUnit1 = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit1"), unitDefinition1.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit battleUnit2 = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit2"), unitDefinition2.Id, BattleSide.Defender, hitpoints: 10);

        BattleState expectedBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(battleUnit1, new GridPosition(0, 0), false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(battleUnit2, new GridPosition(0, 0), false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: new BattleModifiers(
                Attacker: new BattleSideModifiers(ExperiencePercent: 20, AccuracyBonus: 0, RandomDamageBonus: 6),
                Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: -15, RandomDamageBonus: 0)));

        engine.CheckCommand(new WorldMoveStackCommand(stack1.Id, stack2.Position), [
            new WorldStackMovementConsumedEvent(stack1.Id, 3),
            new WorldStackBattleStartedEvent(stack1.Id, stack2.Id, stack2.Position, expectedBattleState, IsDuel: false)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack1.Id, stack1 with
            {
                Direction = WorldStackDirection.SouthEast,
                MovementPoints = 3
            })
        }));
    }

    [Test]
    public void ResolveStackBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("dummy"), dummyDefinition),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("dummy2"), dummyDefinition),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit survivingUnit = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("dummy"),
            DefinitionId: dummyDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 10,
            Experience: 7,
            ExperienceGained: 5,
            Level: 1,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, new GridPosition(0, 0), false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None
        );

        WorldUnit expectedUnit = new(new WorldUnitId("dummy"), dummyDefinition.Id, 10, 12, 1, [], null, false);
        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder().Place(expectedUnit, new GridPosition(0, 0), false).Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommandError(
            new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState with { Outcome = null }, IsDuel: false),
            WorldCommandError.BattleNotFinished);

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        WorldStack expectedStack1 = stack1 with
        {
            Units = expectedAttackerGrid,
            BattlesWon = 1
        };

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack1.Id, expectedStack1).Remove(stack2.Id)
        }));
    }

    [Test]
    public void ResolveStackBattleDeadUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100),
                    GridPosition.BackBottom,
                    false)
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("soldier"), soldierDefinition.Id, BattleSide.Attacker, hitpoints: 0),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(2), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 0),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, expectedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with { Units = expectedAttackerGrid, BattlesWon = 1 })
                    .Remove(stack2.Id)
            }));
    }

    [Test]
    public void ResolveStackBattleSummonedUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition enemyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100),
                    GridPosition.BackBottom,
                    false)
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("soldier"), soldierDefinition.Id, BattleSide.Attacker, hitpoints: 0),
                    GridPosition.FrontBottom,
                    false)
                .PlaceOverlay(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, impDefinition.Id, BattleSide.Attacker, hitpoints: 20) with { SummonerId = new BattleUnitId(0) },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(2), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 0),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, expectedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with { Units = expectedAttackerGrid, BattlesWon = 1 })
                    .Remove(stack2.Id)
            }));
    }

    [Test]
    public void ResolveStackBattleModifiersTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("dummy"), dummyDefinition),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("dummy2"), dummyDefinition),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(TestHelpers.SimpleModifier("modifier1"))
            .WithDefinition(TestHelpers.SimpleModifier("modifier2"))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit survivingUnit = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("dummy"),
            DefinitionId: dummyDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 10,
            Experience: 7,
            ExperienceGained: 5,
            Level: 1,
            Modifiers: [UnitModifier.Permanent(new UnitModifierDefinitionId("modifier1")), UnitModifier.Permanent(new UnitModifierDefinitionId("modifier2"))],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, new GridPosition(0, 0), false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None
        );

        WorldUnit expectedUnit = new(
            new WorldUnitId("dummy"),
            dummyDefinition.Id,
            10,
            12,
            1,
            [UnitModifier.Permanent(new UnitModifierDefinitionId("modifier1")), UnitModifier.Permanent(new UnitModifierDefinitionId("modifier2"))],
            Name: null,
            IsTypeLocked: false);

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder().Place(expectedUnit, new GridPosition(0, 0), false).Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        WorldStack expectedStack1 = stack1 with
        {
            Units = expectedAttackerGrid,
            BattlesWon = 1
        };

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack1.Id, expectedStack1).Remove(stack2.Id)
        }));
    }
}
