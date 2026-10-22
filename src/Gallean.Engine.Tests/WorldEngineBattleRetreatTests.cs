using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineBattleRetreatTests
{
    [Test]
    public void RetreatFromStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

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
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 3);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemyDefinition),
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

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
                    {
                        StatusEffects = [new BattleRetreatedStatusEffect()]
                    },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 10),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Defender, expectedAttackerUnits, expectedDefenderUnits),
            new WorldStackRelocatedEvent(stack1.Id, new WorldPosition(1, 1), new WorldPosition(0, 1), WorldStackDirection.NorthWest)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with
                {
                    Units = expectedAttackerUnits,
                    Position = new WorldPosition(0, 1),
                    Direction = WorldStackDirection.NorthWest
                })
                .SetItem(stack2.Id, stack2 with { Units = expectedDefenderUnits, BattlesWon = 1 })
        }));
    }

    [Test]
    public void NoRetreatPositionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

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
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 3);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemyDefinition),
            movementPoints: 6);

        WorldStack stack3 = TestHelpers.SimpleStack(
            new WorldStackId("Stack3"),
            player2.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy2"), enemyDefinition),
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
            .WithStack(stack3)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
                    {
                        StatusEffects = [new BattleRetreatedStatusEffect()]
                    },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 10),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Defender, expectedAttackerUnits, expectedDefenderUnits)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with { Units = expectedAttackerUnits })
                .SetItem(stack2.Id, stack2 with { Units = expectedDefenderUnits, BattlesWon = 1 })
        }));
    }

    [Test]
    public void DistantBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

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
            new WorldPosition(0, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 3);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(3, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemyDefinition),
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

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
                    {
                        StatusEffects = [new BattleRetreatedStatusEffect()]
                    },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 10),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Defender, expectedAttackerUnits, expectedDefenderUnits),
            new WorldStackRelocatedEvent(stack1.Id, new WorldPosition(0, 1), new WorldPosition(0, 2), WorldStackDirection.SouthWest)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with
                {
                    Units = expectedAttackerUnits,
                    Position = new WorldPosition(0, 2),
                    Direction = WorldStackDirection.SouthWest
                })
                .SetItem(stack2.Id, stack2 with { Units = expectedDefenderUnits, BattlesWon = 1 })
        }));
    }

    [Test]
    public void BagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition enemyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        ItemDefinition itemDefinition = new(
            new ItemDefinitionId("Item"),
            new GlobalTextId("name"),
            new GlobalTextId("description"),
            ItemCategory.Valuable,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 3);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemyDefinition),
            movementPoints: 6);

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(0, 1),
            new WorldObjectVariant(0),
            new WorldInventory([new WorldItem(new WorldItemId("Item1"), itemDefinition.Id, Charges: null)]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .WithBag(bag)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
                    {
                        StatusEffects = [new BattleRetreatedStatusEffect()]
                    },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 10),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Defender, expectedAttackerUnits, expectedDefenderUnits),
            new WorldStackRelocatedEvent(stack1.Id, new WorldPosition(1, 1), new WorldPosition(0, 2), WorldStackDirection.West)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with
                {
                    Units = expectedAttackerUnits,
                    Position = new WorldPosition(0, 2),
                    Direction = WorldStackDirection.West
                })
                .SetItem(stack2.Id, stack2 with { Units = expectedDefenderUnits, BattlesWon = 1 }),
            Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty.Add(bag.Id, bag)
        }));
    }

    [Test]
    public void ZoneOfControlTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

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
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 3);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemyDefinition),
            movementPoints: 6);

        WorldStack stack3 = TestHelpers.SimpleStack(
            new WorldStackId("Stack3"),
            player2.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy2"), enemyDefinition),
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
            .WithStack(stack3)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
                    {
                        StatusEffects = [new BattleRetreatedStatusEffect()]
                    },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 10),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Defender, expectedAttackerUnits, expectedDefenderUnits),
            new WorldStackRelocatedEvent(stack1.Id, new WorldPosition(1, 1), new WorldPosition(0, 1), WorldStackDirection.NorthWest)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with
                {
                    Units = expectedAttackerUnits,
                    Position = new WorldPosition(0, 1),
                    Direction = WorldStackDirection.NorthWest
                })
                .SetItem(stack2.Id, stack2 with { Units = expectedDefenderUnits, BattlesWon = 1 })
                .SetItem(stack3.Id, stack3)
        }));
    }

    [Test]
    public void WrapAroundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

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
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 3);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemyDefinition),
            movementPoints: 6);

        WorldStack stack3 = TestHelpers.SimpleStack(
            new WorldStackId("Stack3"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leaderDefinition),
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
            .WithStack(stack3)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
                    {
                        StatusEffects = [new BattleRetreatedStatusEffect()]
                    },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 10),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Defender, expectedAttackerUnits, expectedDefenderUnits),
            new WorldStackRelocatedEvent(stack1.Id, new WorldPosition(1, 1), new WorldPosition(0, 1), WorldStackDirection.NorthWest)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with
                {
                    Units = expectedAttackerUnits,
                    Position = new WorldPosition(0, 1),
                    Direction = WorldStackDirection.NorthWest
                })
                .SetItem(stack2.Id, stack2 with { Units = expectedDefenderUnits, BattlesWon = 1 })
                .SetItem(stack3.Id, stack3)
        }));
    }

    [Test]
    public void DuelDefenderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition enemyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 60, initiative: 30, damage: 5);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(1, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 3);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 1),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontTop, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(soldierDefinition)
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
                    GridPosition.FrontCenter,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 0),
                    GridPosition.FrontCenter,
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

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontTop, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: true), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerUnits, expectedDefenderUnits),
            new WorldStackRelocatedEvent(stack2.Id, new WorldPosition(2, 1), new WorldPosition(3, 1), WorldStackDirection.SouthEast)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with { Units = expectedAttackerUnits, BattlesWon = 1 })
                .SetItem(stack2.Id, stack2 with
                {
                    Units = expectedDefenderUnits,
                    Position = new WorldPosition(3, 1),
                    Direction = WorldStackDirection.SouthEast
                })
        }));
    }

    [Test]
    public void RetreatFromCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition garrisonDefinition = TestHelpers.SimpleUnitDefinition("garrison", maxHitpoints: 10, initiative: 50, damage: 5);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(4, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 3);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Heretic, 1), WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("garrison"), garrisonDefinition));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(garrisonDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithCity(city)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
                    {
                        StatusEffects = [new BattleRetreatedStatusEffect()]
                    },
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
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("garrison"), garrisonDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveFortificationBattleCommand(stack1.Id, city.Id, finalBattleState), [
            new WorldFortificationBattleEndedEvent(stack1.Id, city.Id, BattleSide.Defender, expectedAttackerUnits, expectedGarrison),
            new WorldStackRelocatedEvent(stack1.Id, new WorldPosition(4, 2), new WorldPosition(5, 3), WorldStackDirection.South)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with
                {
                    Units = expectedAttackerUnits,
                    Position = new WorldPosition(5, 3),
                    Direction = WorldStackDirection.South
                }),
            Fortifications = engine.State.Fortifications.SetItem(city.Id, city with { Garrison = expectedGarrison })
        }));
    }

    [Test]
    public void RetreatFromRuinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition guardDefinition = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 10, initiative: 50, damage: 5);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(2, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 3);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), guardDefinition),
            Resources: WorldResourceSet.Empty,
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(guardDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(stack1)
            .WithRuin(ruin)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
                    {
                        StatusEffects = [new BattleRetreatedStatusEffect()]
                    },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("guard"), guardDefinition.Id, BattleSide.Defender, hitpoints: 10),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guardDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveFortificationBattleCommand(stack1.Id, ruin.Id, finalBattleState), [
            new WorldFortificationBattleEndedEvent(stack1.Id, ruin.Id, BattleSide.Defender, expectedAttackerUnits, expectedGarrison),
            new WorldStackRelocatedEvent(stack1.Id, new WorldPosition(2, 3), new WorldPosition(3, 4), WorldStackDirection.South)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with
                {
                    Units = expectedAttackerUnits,
                    Position = new WorldPosition(3, 4),
                    Direction = WorldStackDirection.South
                }),
            Fortifications = engine.State.Fortifications.SetItem(ruin.Id, ruin with { Garrison = expectedGarrison })
        }));
    }

    [Test]
    public void InsideCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 5);

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
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 3);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(4, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemyDefinition),
            movementPoints: 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .WithCity(city)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
                    {
                        StatusEffects = [new BattleRetreatedStatusEffect()]
                    },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 10),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Defender, expectedAttackerUnits, expectedDefenderUnits)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with { Units = expectedAttackerUnits })
                .SetItem(stack2.Id, stack2 with { Units = expectedDefenderUnits, BattlesWon = 1 })
        }));
    }

    [Test]
    public void DefenderInsideCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 5);

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
            new WorldPosition(4, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 3);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemyDefinition),
            movementPoints: 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Heretic, 1), WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .WithCity(city)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
                    {
                        StatusEffects = [new BattleRetreatedStatusEffect()]
                    },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 10),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Defender, expectedAttackerUnits, expectedDefenderUnits),
            new WorldStackRelocatedEvent(stack1.Id, new WorldPosition(4, 2), new WorldPosition(5, 3), WorldStackDirection.South)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with
                {
                    Units = expectedAttackerUnits,
                    Position = new WorldPosition(5, 3),
                    Direction = WorldStackDirection.South
                })
                .SetItem(stack2.Id, stack2 with { Units = expectedDefenderUnits, BattlesWon = 1 })
        }));
    }

    [Test]
    public void DeadLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition enemyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 300, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(1, 1),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontTop, false)
                .Build(),
            movementPoints: 3);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemyDefinition),
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
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 0),
                    GridPosition.FrontCenter,
                    false)
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("soldier"), soldierDefinition.Id, BattleSide.Attacker, hitpoints: 40) with
                    {
                        StatusEffects = [new BattleRetreatedStatusEffect()]
                    },
                    GridPosition.FrontTop,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(2), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 300),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(2),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Hitpoints = 0 }, GridPosition.FrontCenter, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 40 }, GridPosition.FrontTop, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Defender, expectedAttackerUnits, expectedDefenderUnits),
            new WorldStackRelocatedEvent(stack1.Id, new WorldPosition(1, 1), new WorldPosition(0, 1), WorldStackDirection.NorthWest)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with
                {
                    Units = expectedAttackerUnits,
                    Position = new WorldPosition(0, 1),
                    Direction = WorldStackDirection.NorthWest
                })
                .SetItem(stack2.Id, stack2 with { Units = expectedDefenderUnits, BattlesWon = 1 })
        }));
    }

    [Test]
    public void KilledWhileRetreatingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition enemyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 300, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(1, 1),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontTop, false)
                .Build(),
            movementPoints: 3);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 1),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemy"), enemyDefinition),
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
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Attacker, hitpoints: 0),
                    GridPosition.FrontCenter,
                    false)
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("soldier"), soldierDefinition.Id, BattleSide.Attacker, hitpoints: 0),
                    GridPosition.FrontTop,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(2), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 300),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(2),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None
        );

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Hitpoints = 0 }, GridPosition.FrontCenter, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 0 }, GridPosition.FrontTop, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Defender, expectedAttackerUnits, expectedDefenderUnits),
            new WorldStackDestroyedEvent(stack1.Id, new WorldUnitGroup.Stack(stack2.Id), player2.Id)
        ]);

        Assert.That(engine.State.Stacks.Keys, Is.EqualTo([stack2.Id]));
    }
}
