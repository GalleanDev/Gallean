using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineBattleItemTests
{
    [Test]
    public void ResolveBattleConsumeItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        ItemDefinition usedPotion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);

        ItemDefinition keptItem = new(
            new ItemDefinitionId("orb"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Orb,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem usedPotionItem = new(new WorldItemId("UsedPotion"), usedPotion.Id, Charges: null);

        LeaderEquipment equipment = LeaderEquipment.Empty
            .With(LeaderEquipmentSlot.Battle1, usedPotionItem)
            .With(LeaderEquipmentSlot.Battle2, new WorldItem(new WorldItemId("KeptItem"), keptItem.Id, Charges: null));

        WorldStack stack1 = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player1.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                Equipment = equipment
            };

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy2"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(usedPotion)
            .WithDefinition(keptItem)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit survivingUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, new GridPosition(0, 0), false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items:
            [
                new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, usedPotion.Id, Charges: null, Consumed: true),
                new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle2, keptItem.Id, Charges: null, Consumed: false)
            ],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        WorldUnit expectedUnit = new(new WorldUnitId("dummy"), dummyDefinition.Id, 10, 0, 1, [], null, false);
        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder().Place(expectedUnit, new GridPosition(0, 0), false).Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldItemUsedEvent(stack1.Id, usedPotionItem, RemainingCharges: null, Consumed: true),
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with
                    {
                        Units = expectedAttackerGrid,
                        Equipment = equipment.Without(LeaderEquipmentSlot.Battle1),
                        BattlesWon = 1
                    })
                    .Remove(stack2.Id)
            }));
    }

    [Test]
    public void SummonedUnitAfterBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy2"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(summonDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit survivor = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit summon = new(new BattleUnitId(2), null, summonDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], new BattleUnitId(0), [], null, false);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(survivor, GridPosition.FrontBottom, false)
                .Place(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
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
    public void ResolveBattleSpendTalismanChargesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        ItemDefinition spentTalisman = new(
            new ItemDefinitionId("spent"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        ItemDefinition untouchedTalisman = new(
            new ItemDefinitionId("untouched"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem spentTalismanItem = new(new WorldItemId("SpentTalisman"), spentTalisman.Id, Charges: 5);

        LeaderEquipment equipment = LeaderEquipment.Empty
            .With(LeaderEquipmentSlot.Battle1, spentTalismanItem)
            .With(LeaderEquipmentSlot.Battle2, new WorldItem(new WorldItemId("UntouchedTalisman"), untouchedTalisman.Id, Charges: 5));

        WorldStack stack1 = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player1.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                Equipment = equipment
            };

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy2"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(spentTalisman)
            .WithDefinition(untouchedTalisman)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit survivingUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, new GridPosition(0, 0), false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items:
            [
                new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, spentTalisman.Id, Charges: 3, Consumed: false),
                new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle2, untouchedTalisman.Id, Charges: 5, Consumed: false)
            ],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        WorldUnit expectedUnit = new(new WorldUnitId("dummy"), dummyDefinition.Id, 10, 0, 1, [], null, false);
        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder().Place(expectedUnit, new GridPosition(0, 0), false).Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldItemUsedEvent(stack1.Id, spentTalismanItem, RemainingCharges: 3, Consumed: false),
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with
                    {
                        Units = expectedAttackerGrid,
                        Equipment = equipment.With(LeaderEquipmentSlot.Battle1, new WorldItem(new WorldItemId("SpentTalisman"), spentTalisman.Id, Charges: 3)),
                        BattlesWon = 1
                    })
                    .Remove(stack2.Id)
            }));
    }

    [Test]
    public void ResolveBattleExhaustsTalismanTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem talismanItem = new(new WorldItemId("Talisman"), talisman.Id, Charges: 1);

        LeaderEquipment equipment = LeaderEquipment.Empty
            .With(LeaderEquipmentSlot.Battle1, talismanItem);

        WorldStack stack1 = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player1.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                Equipment = equipment
            };

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy2"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(talisman)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit survivingUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, new GridPosition(0, 0), false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 0, Consumed: true)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        WorldUnit expectedUnit = new(new WorldUnitId("dummy"), dummyDefinition.Id, 10, 0, 1, [], null, false);
        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder().Place(expectedUnit, new GridPosition(0, 0), false).Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldItemUsedEvent(stack1.Id, talismanItem, RemainingCharges: 0, Consumed: true),
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with
                    {
                        Units = expectedAttackerGrid,
                        Equipment = LeaderEquipment.Empty,
                        BattlesWon = 1
                    })
                    .Remove(stack2.Id)
            }));
    }

    [Test]
    public void ModifierExpiresNextTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        UnitModifierDefinition modifierDefinition = TestHelpers.SimpleModifier("modifier") with
        {
            Damage = new UnitStatModifier(0, 30)
        };

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(modifierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit modifiedUnit = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("leader"),
            DefinitionId: leaderDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 100,
            Experience: 0,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [new UnitModifier(modifierDefinition.Id, RemainingTurns: 1)],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("leader"), leaderDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(modifiedUnit, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> modifiedGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Modifiers = [new UnitModifier(modifierDefinition.Id, RemainingTurns: 1)] }, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, modifiedGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with { Units = modifiedGrid, BattlesWon = 1 })
                    .Remove(stack2.Id)
            }));

        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player2.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stack1.Id, 6),
            new WorldUnitModifierExpiredEvent(new WorldUnitGroup.Stack(stack1.Id), new WorldUnitId("leader"), modifierDefinition.Id),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players
                    .SetItem(player1.Id, player1 with { Resources = new WorldResourceSet(100, 25, 0, 0, 0, 0) })
                    .SetItem(player2.Id, player2 with { Resources = new WorldResourceSet(100, 0, 0, 0, 25, 0) }),
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with
                    {
                        Units = new UnitGrid<WorldUnit>.Builder()
                            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                            .Build(),
                        BattlesWon = 1
                    })
                    .Remove(stack2.Id)
            }));
    }

    [Test]
    public void ResolveBattleDestroyedStackItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);

        LeaderEquipment equipment = LeaderEquipment.Empty
            .With(LeaderEquipmentSlot.Battle1, new WorldItem(new WorldItemId("Potion"), potion.Id, Charges: null));

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                player2.Id,
                new WorldPosition(2, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy2"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                Equipment = equipment
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit survivingUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, new GridPosition(0, 0), false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Defender, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        WorldUnit expectedUnit = new(new WorldUnitId("dummy"), dummyDefinition.Id, 10, 0, 1, [], null, false);
        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder().Place(expectedUnit, new GridPosition(0, 0), false).Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldItemUsedEvent(stack2.Id, new WorldItem(new WorldItemId("Potion"), potion.Id, Charges: null), RemainingCharges: null, Consumed: true),
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with { Units = expectedAttackerGrid, BattlesWon = 1 })
                    .Remove(stack2.Id),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty
            }));
    }

    [Test]
    public void ResolveFortificationBattleDestroyedStackItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);

        LeaderEquipment equipment = LeaderEquipment.Empty
            .With(LeaderEquipmentSlot.Battle1, new WorldItem(new WorldItemId("Potion"), potion.Id, Charges: null));

        WorldStack stack1 = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player1.Id,
                new WorldPosition(5, 5),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                Equipment = equipment
            };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy2"), dummyDefinition), new GridPosition(0, 0), false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithCity(city)
            .Build();

        BattleUnit survivingUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleUnit deadAttacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Attacker, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(deadAttacker, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, new GridPosition(0, 0), false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: new BattleModifiers(
                Attacker: BattleSideModifiers.None,
                Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false)));

        WorldUnit expectedUnit = new(new WorldUnitId("dummy"), dummyDefinition.Id, 10, 0, 1, [], null, false);
        UnitGrid<WorldUnit> expectedDefenderGrid = new UnitGrid<WorldUnit>.Builder().Place(expectedUnit, new GridPosition(0, 0), false).Build();

        UnitGrid<WorldUnit> destroyedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveFortificationBattleCommand(stack1.Id, city.Id, finalBattleState), [
            new WorldItemUsedEvent(stack1.Id, new WorldItem(new WorldItemId("Potion"), potion.Id, Charges: null), RemainingCharges: null, Consumed: true),
            new WorldFortificationBattleEndedEvent(stack1.Id, city.Id, BattleSide.Defender, destroyedAttackerGrid, expectedDefenderGrid),
            new WorldStackDestroyedEvent(stack1.Id, new WorldUnitGroup.Fortification(city.Id), player2.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.Remove(stack1.Id),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with { Garrison = expectedDefenderGrid }),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty
            }));
    }
}
