namespace Gallean.Engine.Tests;

public sealed class BattleEngineDrainModifierTests
{
    private static UnitModifierDefinition DrainModifier(string id, int drainPercent)
    {
        return TestHelpers.SimpleModifier(id) with { DrainPercent = drainPercent };
    }

    [Test]
    public void HealsAttackerTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 60, damage: 20);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        UnitModifierDefinition drainModifier = DrainModifier("drain", drainPercent: 25);

        BattleUnit attacker = new(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, 50, 0, 0, 1, [UnitModifier.Permanent(drainModifier.Id)], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(drainModifier)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 20),
            new BattleUnitHealedEvent(attacker.Id, 55),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { Hitpoints = 55 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void DrainAttackTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 60, damage: 20) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 60, damage: 20, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.DrainLife
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        UnitModifierDefinition drainModifier = DrainModifier("drain", drainPercent: 25);

        BattleUnit attacker = new(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, 50, 0, 0, 1, [UnitModifier.Permanent(drainModifier.Id)], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(drainModifier)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 20),
            new BattleUnitHealedEvent(attacker.Id, 65),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { Hitpoints = 65 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void DoesNotOverflowTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 60, damage: 20);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        UnitModifierDefinition drainModifier = DrainModifier("drain", drainPercent: 25);

        BattleUnit attacker = new(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, 98, 0, 0, 1, [UnitModifier.Permanent(drainModifier.Id)], [], null, [], null, false);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(drainModifier)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 20),
            new BattleUnitHealedEvent(attacker.Id, 100),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { Hitpoints = 100 }, GridPosition.FrontBottom, false)
                    .Place(ally, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void DrainOverflowAttackTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 60, damage: 20) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 60, damage: 20, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.DrainLifeOverflow
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        UnitModifierDefinition drainModifier = DrainModifier("drain", drainPercent: 25);

        BattleUnit attacker = new(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, 95, 0, 0, 1, [UnitModifier.Permanent(drainModifier.Id)], [], null, [], null, false);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(drainModifier)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 20),
            new BattleUnitHealedEvent(attacker.Id, 100),
            new BattleUnitHealedEvent(ally.Id, 60),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { Hitpoints = 100 }, GridPosition.FrontBottom, false)
                    .Place(ally with { Hitpoints = 60 }, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void NonDamagingAttackTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 60, damage: 20) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Mind
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        UnitModifierDefinition drainModifier = DrainModifier("drain", drainPercent: 25);

        BattleUnit attacker = new(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, 50, 0, 0, 1, [UnitModifier.Permanent(drainModifier.Id)], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(drainModifier)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 0),
            new BattleUnitEffectAppliedEvent(attacker.Id, defender.Id, new BattleParalyzeStatusEffect(1)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattleParalyzeStatusEffect(0)),
            new BattleUnitTurnSkippedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void DrainAttackRoundingTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 60, damage: 45) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 60, damage: 45, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.DrainLife
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        UnitModifierDefinition drainModifier = DrainModifier("drain", drainPercent: 10);

        BattleUnit attacker = new(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, 50, 0, 0, 1, [UnitModifier.Permanent(drainModifier.Id)], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(drainModifier)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 45),
            new BattleUnitHealedEvent(attacker.Id, 76),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { Hitpoints = 76 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 55 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void SecondaryAttackTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 60, damage: 20) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("primary"), initiative: 60, damage: 20, accuracy: 100, reach: UnitAttackReach.Any),
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("secondary"), initiative: 60, damage: 10, accuracy: 100, reach: UnitAttackReach.Any)
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 200, initiative: 40, damage: 5);

        UnitModifierDefinition weaponModifier = DrainModifier("weapon", drainPercent: 25) with { Damage = new UnitStatModifier(0, 50) };

        BattleUnit attacker = new(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, 50, 0, 0, 1, [UnitModifier.Permanent(weaponModifier.Id)], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(weaponModifier)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("primary"), new UnitAttackDefinitionId("secondary")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("primary")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("primary"), 30),
            new BattleUnitHealedEvent(attacker.Id, 57),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("secondary")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("secondary"), 10),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { Hitpoints = 57 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 160 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void ItemAttackTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitAttackDefinition scrollAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("scroll"), initiative: 50, damage: 40, accuracy: 100, reach: UnitAttackReach.Any);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 200, initiative: 40, damage: 5);

        UnitModifierDefinition weaponModifier = DrainModifier("weapon", drainPercent: 25) with { Damage = new UnitStatModifier(0, 50) };

        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(scrollAttack.Id),
            AnimationUnitId: null);

        BattleUnit leader = new(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, 50, 0, 0, 1, [UnitModifier.Permanent(weaponModifier.Id)], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(leaderDefinition)
            .WithDefinition(scrollAttack)
            .WithDefinition(defenderDefinition)
            .WithDefinition(weaponModifier)
            .WithDefinition(talisman)
            .WithAttacker(leader, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 5, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(defender.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, leader.Id, new BattleAttackUnitTarget(defender.Id), RemainingCharges: 4, Consumed: false),
            new BattleUnitAttackedEvent(leader.Id, defender.Id, new UnitAttackDefinitionId("scroll")),
            new BattleUnitAttackHitEvent(leader.Id, defender.Id, new UnitAttackDefinitionId("scroll"), 40),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleUnitTurnEndedEvent(leader.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 160 }, GridPosition.FrontBottom, false)
                    .Build(),
                Items = [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 4, Consumed: false)],
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }
}
