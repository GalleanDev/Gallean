namespace Gallean.Engine.Tests;

public sealed class WorldEngineDiplomacyAttackTests
{
    private static WorldRelation Relation(WorldRace race, WorldRace otherRace, int current)
    {
        return new WorldRelation(
            new WorldRelationId(race, otherRace),
            current,
            Previous: current,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
    }

    [Test]
    public void AttackStackTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "..."
        ]);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldRelation relation = Relation(WorldRace.Undead, WorldRace.Heretic, current: 40);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithStack(enemyStack)
            .WithRelation(relation)
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
            Modifiers: BattleModifiers.None);

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)),
            [
                new WorldStackMovementConsumedEvent(stack.Id, 3),
                new WorldStackBattleStartedEvent(stack.Id, enemyStack.Id, enemyStack.Position, expectedBattleState, IsDuel: false),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 33)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with { Direction = WorldStackDirection.SouthEast, MovementPoints = 3 }),
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 33 })
            }));
    }

    [Test]
    public void AttackStackAtPeaceTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "..."
        ]);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldRelation relation = Relation(WorldRace.Undead, WorldRace.Heretic, current: 90);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithStack(enemyStack)
            .WithRelation(relation)
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
            Modifiers: BattleModifiers.None);

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)),
            [
                new WorldStackMovementConsumedEvent(stack.Id, 3),
                new WorldStackBattleStartedEvent(stack.Id, enemyStack.Id, enemyStack.Position, expectedBattleState, IsDuel: false),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 66)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with { Direction = WorldStackDirection.SouthEast, MovementPoints = 3 }),
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 66 })
            }));
    }

    [Test]
    public void AttackNeutralStackTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "..."
        ]);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack neutralStack = TestHelpers.SimpleStack(
            new WorldStackId("neutral"),
            neutral.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldRelation relation = Relation(WorldRace.Undead, WorldRace.Neutral, current: 40);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(stack)
            .WithStack(neutralStack)
            .WithRelation(relation)
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
            Modifiers: BattleModifiers.None);

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)),
            [
                new WorldStackMovementConsumedEvent(stack.Id, 3),
                new WorldStackBattleStartedEvent(stack.Id, neutralStack.Id, neutralStack.Position, expectedBattleState, IsDuel: false)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with { Direction = WorldStackDirection.SouthEast, MovementPoints = 3 })
            }));
    }

    [Test]
    public void AttackCityTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "........",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........",
            "........"
        ]);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader));

        WorldRelation relation = Relation(WorldRace.Undead, WorldRace.Heretic, current: 40);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithCity(city)
            .WithRelation(relation)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: 100), GridPosition.FrontBottom)
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("leader2"), leader.Id, BattleSide.Defender, hitpoints: 100), GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(3, 3)),
            [
                new WorldStackMovementConsumedEvent(stack.Id, 5),
                new WorldFortificationBattleStartedEvent(stack.Id, city.Id, city.Entrance, expectedBattleState),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 30)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with { Direction = WorldStackDirection.North, MovementPoints = 5 }),
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 30 })
            }));
    }

    [Test]
    public void AttackStackEnemiesTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "..."
        ]);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldRelation relation = Relation(WorldRace.Undead, WorldRace.Heretic, current: 40);
        WorldRelation enemyRelation = Relation(WorldRace.Undead, WorldRace.Human, current: 50);
        WorldRelation targetEnemyRelation = Relation(WorldRace.Human, WorldRace.Heretic, current: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(enemyStack)
            .WithRelation(relation)
            .WithRelation(enemyRelation)
            .WithRelation(targetEnemyRelation)
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
            Modifiers: BattleModifiers.None);

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)),
            [
                new WorldStackMovementConsumedEvent(stack.Id, 3),
                new WorldStackBattleStartedEvent(stack.Id, enemyStack.Id, enemyStack.Position, expectedBattleState, IsDuel: false),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 33),
                new WorldRelationChangedEvent(player1.Id, enemy.Id, 53)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with { Direction = WorldStackDirection.SouthEast, MovementPoints = 3 }),
                Relations = before.Relations
                    .SetItem(relation.Id, relation with { Current = 33 })
                    .SetItem(enemyRelation.Id, enemyRelation with { Current = 53 })
            }));
    }

    [Test]
    public void AttackStackAtPeaceEnemiesTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "..."
        ]);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        WorldRelation relation = Relation(WorldRace.Undead, WorldRace.Heretic, current: 90);
        WorldRelation enemyRelation = Relation(WorldRace.Undead, WorldRace.Human, current: 50);
        WorldRelation targetEnemyRelation = Relation(WorldRace.Human, WorldRace.Heretic, current: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(enemyStack)
            .WithRelation(relation)
            .WithRelation(enemyRelation)
            .WithRelation(targetEnemyRelation)
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
            Modifiers: BattleModifiers.None);

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)),
            [
                new WorldStackMovementConsumedEvent(stack.Id, 3),
                new WorldStackBattleStartedEvent(stack.Id, enemyStack.Id, enemyStack.Position, expectedBattleState, IsDuel: false),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 66),
                new WorldRelationChangedEvent(player1.Id, enemy.Id, 17)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.SetItem(stack.Id, stack with { Direction = WorldStackDirection.SouthEast, MovementPoints = 3 }),
                Relations = before.Relations
                    .SetItem(relation.Id, relation with { Current = 66 })
                    .SetItem(enemyRelation.Id, enemyRelation with { Current = 17 })
            }));
    }
}
