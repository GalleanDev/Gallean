namespace Gallean.Engine.Tests;

public sealed class BattleEngineGetItemInfoTests
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
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
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
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        BattleGetItemInfoCommand.Result result = engine.CheckCommand(new BattleGetItemInfoCommand(leader.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetItemInfoCommand.Result(
        [
            new BattleGetItemInfoCommand.SlotInfo(LeaderEquipmentSlot.Battle1,
            [
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(leader.Id), [new BattleAttackUnitTarget(leader.Id)], null),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(ally.Id), [], BattleCommandError.TargetNotHealable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(defender.Id), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.ItemTargetInvalid)
            ])
        ])));
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

        BattleGetItemInfoCommand.Result result = engine.CheckCommand(new BattleGetItemInfoCommand(leader.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetItemInfoCommand.Result(
        [
            new BattleGetItemInfoCommand.SlotInfo(LeaderEquipmentSlot.Battle1,
            [
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(leader.Id), [], BattleCommandError.TargetIncorrect),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(defender.Id), [new BattleAttackUnitTarget(defender.Id), new BattleAttackUnitTarget(secondDefender.Id)], null),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(secondDefender.Id), [new BattleAttackUnitTarget(defender.Id), new BattleAttackUnitTarget(secondDefender.Id)], null),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.AttackRequiresUnitTarget),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.AttackRequiresUnitTarget),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.AttackRequiresUnitTarget),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.AttackRequiresUnitTarget),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.AttackRequiresUnitTarget)
            ])
        ])));
    }

    [Test]
    public void SummonTalismanOverCorpseTest()
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
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 40, initiative: 40, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 30, damage: 5);
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
        BattleUnit corpse = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(summonDefinition)
            .WithDefinition(summonAttack)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(talisman)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(corpse, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 5, Consumed: false))
            .Build();

        BattleGetItemInfoCommand.Result result = engine.CheckCommand(new BattleGetItemInfoCommand(leader.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetItemInfoCommand.Result(
        [
            new BattleGetItemInfoCommand.SlotInfo(LeaderEquipmentSlot.Battle1,
            [
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(leader.Id), [], BattleCommandError.SummonTargetOccupied),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(corpse.Id), [new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)], null),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(defender.Id), [], BattleCommandError.SummonTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop)], null),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom), [new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom)], null),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter)], null),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop)], null),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.SummonTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop), [], BattleCommandError.SummonTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.SummonTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.SummonTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.SummonTargetInvalid)
            ])
        ])));
    }

    [Test]
    public void SummonedStackLeaderTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Summon,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
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
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        BattleGetItemInfoCommand.Result result = engine.CheckCommand(new BattleGetItemInfoCommand(leader.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetItemInfoCommand.Result(
        [
            new BattleGetItemInfoCommand.SlotInfo(LeaderEquipmentSlot.Battle1,
            [
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(leader.Id), [new BattleAttackUnitTarget(leader.Id)], null),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(ally.Id), [], BattleCommandError.TargetNotHealable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(defender.Id), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.ItemTargetInvalid)
            ])
        ])));
    }

    [Test]
    public void NotItemUserTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 60, damage: 5);
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
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(new BattleGetItemInfoCommand(ally.Id), BattleCommandError.ItemUserNotLeader);
    }

    [Test]
    public void ConsumedSlotTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
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
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true))
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle2, potion.Id, Charges: null, Consumed: false))
            .Build();

        BattleGetItemInfoCommand.Result result = engine.CheckCommand(new BattleGetItemInfoCommand(leader.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetItemInfoCommand.Result(
        [
            new BattleGetItemInfoCommand.SlotInfo(LeaderEquipmentSlot.Battle2,
            [
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(leader.Id), [new BattleAttackUnitTarget(leader.Id)], null),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(ally.Id), [], BattleCommandError.TargetNotHealable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(defender.Id), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.ItemTargetInvalid)
            ])
        ])));
    }

    [Test]
    public void NotUnitTurnTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 60, damage: 5);
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
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommandError(new BattleGetItemInfoCommand(leader.Id), BattleCommandError.NotUnitTurn);
    }
}
