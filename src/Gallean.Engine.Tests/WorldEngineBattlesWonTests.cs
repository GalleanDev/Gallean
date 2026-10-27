namespace Gallean.Engine.Tests;

public sealed class WorldEngineBattlesWonTests
{
    [Test]
    public void WinningStackBattleTest()
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

        WorldStack stack1 = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player1.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
                movementPoints: 6) with
            {
                BattlesWon = 2
            };

        WorldStack stack2 = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                player2.Id,
                new WorldPosition(2, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemyDefinition),
                movementPoints: 6) with
            {
                BattlesWon = 5
            };

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

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 0),
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
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
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
                    .SetItem(stack1.Id, stack1 with { Units = expectedAttackerGrid, BattlesWon = 3 })
                    .Remove(stack2.Id)
            }));
    }

    [Test]
    public void LosingFortificationBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        UnitDefinition garrisonDefinition = TestHelpers.SimpleUnitDefinition("garrison", maxHitpoints: 10, initiative: 50, damage: 5);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                attacker.Id,
                new WorldPosition(2, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
                movementPoints: 2) with
            {
                BattlesWon = 2
            };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital1"),
            defender.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("garrison"), garrisonDefinition));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(garrisonDefinition)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(stack)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 40) with { StatusEffects = [new BattleRetreatedStatusEffect()] },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("garrison"), garrisonDefinition.Id, BattleSide.Defender, hitpoints: 10),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: new BattleModifiers(
                Attacker: BattleSideModifiers.None,
                Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false)));

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Hitpoints = 40 }, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("garrison"), garrisonDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(stack.Id, capital.Id, finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    stack.Id,
                    capital.Id,
                    BattleSide.Defender,
                    expectedAttackerGrid,
                    expectedGarrison)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with { Units = expectedAttackerGrid }),
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with { Garrison = expectedGarrison })
            }));
    }
}
