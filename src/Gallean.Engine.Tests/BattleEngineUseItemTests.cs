namespace Gallean.Engine.Tests;

public sealed class BattleEngineUseItemTests
{
    [Test]
    public void HealPotionTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
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

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, leader.Id, new BattleAttackUnitTarget(leader.Id), RemainingCharges: null, Consumed: true),
            new BattleUnitHealedEvent(leader.Id, 90),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(leader with { Hitpoints = 90 }, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));

        engine.CheckCommandError(new BattleDefendCommand(leader.Id), BattleCommandError.NotUnitTurn);
    }

    [Test]
    public void HealPotionMaxHitpointsTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(80),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 70);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, leader.Id, new BattleAttackUnitTarget(leader.Id), RemainingCharges: null, Consumed: true),
            new BattleUnitHealedEvent(leader.Id, 100),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(leader with { Hitpoints = 100 }, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void NotUnitTurnTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5);
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

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, leaderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(potion)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(leader, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Defender, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(attacker.Id)), BattleCommandError.NotUnitTurn);
    }

    [Test]
    public void NotLeaderTest()
    {
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
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

        BattleUnit soldier = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, soldierDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(soldierDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(soldier, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(new BattleUseItemCommand(soldier.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(soldier.Id)), BattleCommandError.ItemUserNotLeader);
    }

    [Test]
    public void EmptySlotTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
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

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle2, new BattleAttackUnitTarget(leader.Id)), BattleCommandError.ItemNotEquipped);
    }

    [Test]
    public void AlreadyConsumedTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
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

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true))
            .Build();

        engine.CheckCommandError(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)), BattleCommandError.ItemNotEquipped);
    }

    [Test]
    public void MissingLeaderAbilityTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition orb = new(
            new ItemDefinitionId("orb"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Orb,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(orb)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, orb.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)), BattleCommandError.MissingLeaderAbility);
    }

    [Test]
    public void NotUsableTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition revivePotion = new(
            new ItemDefinitionId("revive"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionRevive,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(revivePotion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, revivePotion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)), BattleCommandError.ItemNotUsable);
    }

    [Test]
    public void OrbWithAbilityTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.OrbUse,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition orb = new(
            new ItemDefinitionId("orb"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Orb,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(orb)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, orb.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)), BattleCommandError.ItemNotUsable);
    }

    [Test]
    public void HealPotionAllyTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 30, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
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

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontTop)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(ally.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, leader.Id, new BattleAttackUnitTarget(ally.Id), RemainingCharges: null, Consumed: true),
            new BattleUnitHealedEvent(ally.Id, 60),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Place(ally with { Hitpoints = 60 }, GridPosition.FrontTop, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void HealPotionEmptyTargetTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
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

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(
            new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop)),
            BattleCommandError.ItemTargetInvalid);
    }

    [Test]
    public void RevivePotionTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 80, initiative: 30, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionRevive,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new ReviveItemEffect(),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontTop)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(ally.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, leader.Id, new BattleAttackUnitTarget(ally.Id), RemainingCharges: null, Consumed: true),
            new BattleUnitRevivedEvent(ally.Id, 1),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Place(ally with { Hitpoints = 1, IsRevived = true }, GridPosition.FrontTop, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void RevivePotionLivingTargetTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 80, initiative: 30, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionRevive,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new ReviveItemEffect(),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontTop)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(
            new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.ItemTargetInvalid);
    }

    [Test]
    public void RevivePotionEnemyCorpseTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionRevive,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new ReviveItemEffect(),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(
            new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(defender.Id)),
            BattleCommandError.ItemTargetInvalid);
    }

    [Test]
    public void RevivePotionAlreadyRevivedTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 80, initiative: 30, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionRevive,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new ReviveItemEffect(),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0) with { IsRevived = true };
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontTop)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(
            new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.ItemTargetInvalid);
    }

    [Test]
    public void SummonTalismanTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitAttackDefinition summonAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summon"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Type = UnitAttackType.Summon,
            SummonUnitIds = [summonDefinition.Id]
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(summonAttack.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(summonDefinition)
            .WithDefinition(summonAttack)
            .WithDefinition(defenderDefinition)
            .WithDefinition(talisman)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 5, Consumed: false))
            .Build();

        BattleUnit summon = new(new BattleUnitId(2), null, summonDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], leader.Id, [], null, false);

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, leader.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), RemainingCharges: 4, Consumed: false),
            new BattleUnitSummonedEvent(summon.Id, summonDefinition.Id, BattleSide.Attacker, GridPosition.FrontTop, 20, 1, leader.Id),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .PlaceOverlay(summon, GridPosition.FrontTop, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 4, Consumed: false)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SummonTargetOccupiedTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitAttackDefinition summonAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summon"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Type = UnitAttackType.Summon,
            SummonUnitIds = [summonDefinition.Id]
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(summonAttack.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(summonDefinition)
            .WithDefinition(summonAttack)
            .WithDefinition(defenderDefinition)
            .WithDefinition(talisman)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 5, Consumed: false))
            .Build();

        engine.CheckCommandError(
            new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)),
            BattleCommandError.SummonTargetOccupied);
    }

    [Test]
    public void SummonWrongSideTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitAttackDefinition summonAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summon"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Type = UnitAttackType.Summon,
            SummonUnitIds = [summonDefinition.Id]
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(summonAttack.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(summonDefinition)
            .WithDefinition(summonAttack)
            .WithDefinition(defenderDefinition)
            .WithDefinition(talisman)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 5, Consumed: false))
            .Build();

        engine.CheckCommandError(
            new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop)),
            BattleCommandError.SummonTargetInvalid);
    }

    [Test]
    public void SummonActsAndAttacksTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 60, damage: 8);
        UnitAttackDefinition summonAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summon"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Type = UnitAttackType.Summon,
            SummonUnitIds = [summonDefinition.Id]
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(summonAttack.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(summonDefinition)
            .WithDefinition(summonAttack)
            .WithDefinition(defenderDefinition)
            .WithDefinition(talisman)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 5, Consumed: false))
            .Build();

        BattleUnitId summonId = new(2);
        BattleUnit summon = new(summonId, null, summonDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], leader.Id, [], null, false);
        BattleItem talismanItem = new(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 4, Consumed: false);

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, leader.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), RemainingCharges: 4, Consumed: false),
            new BattleUnitSummonedEvent(summonId, summonDefinition.Id, BattleSide.Attacker, GridPosition.FrontCenter, 20, 1, leader.Id),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [talismanItem],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));

        engine.CheckCommand(new BattleSkipCommand(defender.Id), [
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(summonId, BattleUnitTurnType.Normal), new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summonId, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(summonId, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [talismanItem],
            CurrentRound: BattleRound.Normal(2),
            Outcome: null,
            Modifiers: BattleModifiers.None)));

        engine.CheckCommand(new BattleAttackCommand(summonId, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(summonId, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("imp")]),
            new BattleUnitAttackedEvent(summonId, defender.Id, new UnitAttackDefinitionId("imp")),
            new BattleUnitAttackHitEvent(summonId, defender.Id, new UnitAttackDefinitionId("imp"), 8),
            new BattleUnitTurnEndingEvent(summonId),
            new BattleUnitTurnEndedEvent(summonId),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender with { Hitpoints = 2 }, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [talismanItem],
            CurrentRound: BattleRound.Normal(2),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void BoostPotionTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitModifierDefinition modifier = TestHelpers.SimpleModifier("boost") with
        {
            Armor = new UnitStatModifier(30, 0)
        };
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionTemporaryBoost,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new TemporaryBoostItemEffect(modifier.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(modifier)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, leader.Id, new BattleAttackUnitTarget(leader.Id), RemainingCharges: null, Consumed: true),
            new BattleUnitModifierAppliedEvent(leader.Id, new UnitModifier(modifier.Id, 1)),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(leader with { Modifiers = [new UnitModifier(modifier.Id, 1)] }, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void WardingPotionTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitWard ward = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once);
        UnitModifierDefinition modifier = TestHelpers.SimpleModifier("fire-ward") with
        {
            Wards = [ward]
        };
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionTemporaryBoost,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new TemporaryBoostItemEffect(modifier.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(modifier)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, leader.Id, new BattleAttackUnitTarget(leader.Id), RemainingCharges: null, Consumed: true),
            new BattleUnitModifierAppliedEvent(leader.Id, new UnitModifier(modifier.Id, 1)),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit warded = leader with
        {
            Modifiers = [new UnitModifier(modifier.Id, 1)],
            Wards = [ward]
        };

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(warded, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void BoostPotionTwiceTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitModifierDefinition modifier = TestHelpers.SimpleModifier("boost") with
        {
            Armor = new UnitStatModifier(30, 0)
        };
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionTemporaryBoost,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new TemporaryBoostItemEffect(modifier.Id),
            AnimationUnitId: null);

        BattleUnit leader = new(
            new BattleUnitId(0),
            null,
            leaderDefinition.Id,
            BattleSide.Attacker,
            100,
            0,
            0,
            1,
            [new UnitModifier(modifier.Id, 1)],
            [],
            null,
            [],
            Name: null,
            IsTypeLocked: false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(modifier)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(
            new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)),
            BattleCommandError.UnitModifierAlreadyApplied);
    }

    [Test]
    public void ModifierLimitTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitDefinition protectorDefinition = TestHelpers.SimpleUnitDefinition("protector", maxHitpoints: 50, initiative: 30, damage: 5);
        UnitModifierDefinition modifier = TestHelpers.SimpleModifier("boost") with
        {
            Armor = new UnitStatModifier(30, 0)
        };
        UnitModifierDefinition permanentModifier = TestHelpers.SimpleModifier("permanent") with
        {
            Armor = new UnitStatModifier(10, 0)
        };
        UnitWard fireWard = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once);
        UnitModifierDefinition wardModifier = TestHelpers.SimpleModifier("ward") with
        {
            Wards = [fireWard]
        };
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionTemporaryBoost,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new TemporaryBoostItemEffect(modifier.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100) with
        {
            Modifiers = [new UnitModifier(permanentModifier.Id, null)],
            Wards = [fireWard],
            WardModifiers = [new BattleUnitWardModifier(new BattleUnitId(2), wardModifier.Id, fireWard)]
        };
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);
        BattleUnit protector = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, protectorDefinition.Id, BattleSide.Attacker, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(protectorDefinition)
            .WithDefinition(modifier)
            .WithDefinition(permanentModifier)
            .WithDefinition(wardModifier)
            .WithDefinition(potion)
            .WithGlobals(new GlobalVariables { BattleInitiativeVariance = 0, BattleDamageVariance = 0 })
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(protector, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, leader.Id, new BattleAttackUnitTarget(leader.Id), RemainingCharges: null, Consumed: true),
            new BattleUnitModifierAppliedEvent(leader.Id, new UnitModifier(modifier.Id, 1)),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit boosted = leader with
        {
            Modifiers = [new UnitModifier(permanentModifier.Id, null), new UnitModifier(modifier.Id, 1)]
        };

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(boosted, GridPosition.FrontBottom, false).Place(protector, GridPosition.BackBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(protector.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void HealingOrbTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.OrbUse,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitAttackDefinition healAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("heal"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Type = UnitAttackType.Heal,
            Heal = 100
        };
        ItemDefinition orb = new(
            new ItemDefinitionId("orb"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Orb,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(healAttack.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(healAttack)
            .WithDefinition(orb)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontTop)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, orb.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(ally.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, orb.Id, leader.Id, new BattleAttackUnitTarget(ally.Id), RemainingCharges: null, Consumed: false),
            new BattleUnitAttackedEvent(leader.Id, ally.Id, new UnitAttackDefinitionId("heal")),
            new BattleUnitAttackHitEvent(leader.Id, ally.Id, new UnitAttackDefinitionId("heal"), 0),
            new BattleUnitHealedEvent(ally.Id, 100),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(leader, GridPosition.FrontBottom, false)
                .Place(ally with { Hitpoints = 100 }, GridPosition.FrontTop, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, orb.Id, Charges: null, Consumed: false)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void DamageTalismanTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitAttackDefinition fireAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("fire"), initiative: 50, damage: 25, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Source = UnitAttackSource.Fire
        };
        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(fireAttack.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(fireAttack)
            .WithDefinition(talisman)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 5, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(defender.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, leader.Id, new BattleAttackUnitTarget(defender.Id), RemainingCharges: 4, Consumed: false),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, fireAttack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, fireAttack.Id, 25),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(leader, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender with { Hitpoints = 75 }, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 4, Consumed: false)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void DamageTalismanAllReachTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitDefinition secondDefenderDefinition = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 100, initiative: 30, damage: 5);
        UnitAttackDefinition frostAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("frost"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.All) with
        {
            Source = UnitAttackSource.Water
        };
        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(frostAttack.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);
        BattleUnit secondDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, secondDefenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(secondDefenderDefinition)
            .WithDefinition(frostAttack)
            .WithDefinition(talisman)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithDefender(secondDefender, GridPosition.FrontTop)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 5, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(defender.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, leader.Id, new BattleAttackUnitTarget(defender.Id), RemainingCharges: 4, Consumed: false),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, frostAttack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, frostAttack.Id, 10),
            new BattleUnitAttackedEvent(leader.Id, secondDefender.Id, frostAttack.Id),
            new BattleUnitAttackHitEvent(leader.Id, secondDefender.Id, frostAttack.Id, 10),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(leader, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 90 }, GridPosition.FrontBottom, false)
                .Place(secondDefender with { Hitpoints = 90 }, GridPosition.FrontTop, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(secondDefender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 4, Consumed: false)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void OrbProtectedBackLineTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("leader"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.Adjacent),
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.OrbUse,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 10, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 30, damage: 5);
        UnitDefinition backDefenderDefinition = TestHelpers.SimpleUnitDefinition("back", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitAttackDefinition fireAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("fire"), initiative: 50, damage: 25, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Source = UnitAttackSource.Fire
        };
        ItemDefinition orb = new(
            new ItemDefinitionId("orb"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Orb,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(fireAttack.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);
        BattleUnit backDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, backDefenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(backDefenderDefinition)
            .WithDefinition(fireAttack)
            .WithDefinition(orb)
            .WithAttacker(leader, GridPosition.BackBottom)
            .WithAttacker(ally, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithDefender(backDefender, GridPosition.BackTop)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, orb.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(leader.Id, new BattleAttackUnitTarget(backDefender.Id)),
            BattleCommandError.TargetNotReachable);

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(backDefender.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, orb.Id, leader.Id, new BattleAttackUnitTarget(backDefender.Id), RemainingCharges: null, Consumed: false),
            new BattleUnitAttackedEvent(leader.Id, backDefender.Id, fireAttack.Id),
            new BattleUnitAttackHitEvent(leader.Id, backDefender.Id, fireAttack.Id, 25),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(leader, GridPosition.BackBottom, false)
                .Place(ally, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender, GridPosition.FrontBottom, false)
                .Place(backDefender with { Hitpoints = 75 }, GridPosition.BackTop, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(backDefender.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, orb.Id, Charges: null, Consumed: false)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void DamageTalismanUserStatsTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitAttackDefinition fireAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("fire"), initiative: 50, damage: 25, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Source = UnitAttackSource.Fire
        };
        UnitDynamicUpgradeDefinition dynamicUpgrade = new(
            leaderDefinition.Id,
            UpgradeChangeLevel: 10,
            LowLevelUpgrade: UnitDynamicUpgrade.Empty with { Damage = 10 },
            HighLevelUpgrade: UnitDynamicUpgrade.Empty);
        UnitModifierDefinition damageBonus = TestHelpers.SimpleModifier("damage") with
        {
            Damage = new UnitStatModifier(0, 100)
        };
        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(fireAttack.Id),
            AnimationUnitId: null);

        BattleUnit leader = new(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, 100, 0, 0, 10, [UnitModifier.Permanent(damageBonus.Id)], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(fireAttack)
            .WithDefinition(dynamicUpgrade)
            .WithDefinition(damageBonus)
            .WithDefinition(talisman)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 5, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(defender.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, leader.Id, new BattleAttackUnitTarget(defender.Id), RemainingCharges: 4, Consumed: false),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, fireAttack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, fireAttack.Id, 25),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(leader, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender with { Hitpoints = 75 }, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 4, Consumed: false)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void TalismanLastChargeTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitAttackDefinition fireAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("fire"), initiative: 50, damage: 25, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Source = UnitAttackSource.Fire
        };
        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(fireAttack.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(fireAttack)
            .WithDefinition(talisman)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 1, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(defender.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, leader.Id, new BattleAttackUnitTarget(defender.Id), RemainingCharges: 0, Consumed: true),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, fireAttack.Id),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, fireAttack.Id, 25),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(leader, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender with { Hitpoints = 75 }, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 0, Consumed: true)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void ScrollTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.WandScrollUse,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        ItemDefinition scroll = new(
            new ItemDefinitionId("scroll"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Scroll,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new SpellItemEffect(new WorldSpellDefinitionId("heal"), WorldResourceSet.Empty),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(scroll)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, scroll.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)), BattleCommandError.ItemNotUsable);
    }

    [Test]
    public void HealPotionFullHitpointsTargetTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
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

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(
            new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)),
            BattleCommandError.TargetNotHealable);
    }

    [Test]
    public void SummonedStackLeaderTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Summon,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
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

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 40);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, leader.Id, new BattleAttackUnitTarget(leader.Id), RemainingCharges: null, Consumed: true),
            new BattleUnitHealedEvent(leader.Id, 90),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(leader with { Hitpoints = 90 }, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true)],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));

        engine.CheckCommandError(new BattleDefendCommand(leader.Id), BattleCommandError.NotUnitTurn);
    }

    [Test]
    public void TransformedLeaderTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition formDefinition = TestHelpers.SimpleUnitDefinition("form", maxHitpoints: 100, initiative: 50, damage: 5) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
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

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, formDefinition.Id, BattleSide.Attacker, hitpoints: 40) with
        {
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.Transform, leaderDefinition.Id, 1, [], null)]
        };
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(formDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(leader.Id)), BattleCommandError.ItemUserNotLeader);
    }

    [Test]
    public void BattleSummonTest()
    {
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("summon", maxHitpoints: 100, initiative: 50, damage: 5) with
        {
            Category = UnitCategory.Summon,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
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

        BattleUnit summon = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonDefinition.Id, BattleSide.Attacker, hitpoints: 40) with { SummonerId = new BattleUnitId(2) };
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(summon, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(new BattleUseItemCommand(summon.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(summon.Id)), BattleCommandError.ItemUserNotLeader);
    }

    [Test]
    public void HealPotionRetreatedTargetTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 30, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
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

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = new(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, 10, 0, 0, 1, [], [new BattleRetreatedStatusEffect()], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontTop)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(
            new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.ItemTargetInvalid);
    }

    [Test]
    public void BoostPotionRetreatedTargetTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 30, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitModifierDefinition modifier = TestHelpers.SimpleModifier("boost") with
        {
            Armor = new UnitStatModifier(30, 0)
        };
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionTemporaryBoost,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new TemporaryBoostItemEffect(modifier.Id),
            AnimationUnitId: null);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = new(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [new BattleRetreatedStatusEffect()], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(modifier)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontTop)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(
            new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.ItemTargetInvalid);
    }
}
