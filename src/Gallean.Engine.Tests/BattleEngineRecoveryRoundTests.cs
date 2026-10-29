using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class BattleEngineRecoveryRoundTests
{
    private static UnitDefinition HealerDefinition(string id, int maxHitpoints, int initiative, int heal, UnitAttackReach reach)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints, initiative, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id), initiative, damage: 0, accuracy: 100, reach: reach) with
            {
                Type = UnitAttackType.Heal,
                Heal = heal
            }
        };
    }

    private static UnitDefinition SupportDefinition(string id, int maxHitpoints, int initiative, UnitAttackType type, int heal)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints, initiative, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id), initiative, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = type,
                Heal = heal
            }
        };
    }

    [Test]
    public void HealsSurvivorTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition clericDefinition = HealerDefinition("cleric", maxHitpoints: 100, initiative: 40, heal: 30, UnitAttackReach.Any);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(clericDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(cleric, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Recovery(1), [new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior, GridPosition.FrontBottom, false)
                .Place(cleric, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));

        engine.CheckCommand(new BattleAttackCommand(cleric.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(cleric.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("cleric")]),
            new BattleUnitAttackedEvent(cleric.Id, warrior.Id, new UnitAttackDefinitionId("cleric")),
            new BattleUnitAttackHitEvent(cleric.Id, warrior.Id, new UnitAttackDefinitionId("cleric"), 0),
            new BattleUnitHealedEvent(warrior.Id, 80),
            new BattleUnitTurnEndingEvent(cleric.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(cleric.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { Hitpoints = 80, ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(cleric with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void NoWoundedTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition clericDefinition = HealerDefinition("cleric", maxHitpoints: 100, initiative: 40, heal: 30, UnitAttackReach.Any);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(clericDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(cleric, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(cleric.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(cleric with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void NoHealerTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(ally.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(ally with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void AllReachHealTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition clericDefinition = HealerDefinition("cleric", maxHitpoints: 100, initiative: 40, heal: 30, UnitAttackReach.All);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 60);
        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 80);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(clericDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontTop)
            .WithAttacker(cleric, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Recovery(1), [new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(cleric.Id, new BattleAttackUnitTarget(cleric.Id)), [
            new BattleUnitAttackEvent(cleric.Id, new BattleAttackUnitTarget(cleric.Id), [new UnitAttackDefinitionId("cleric")]),
            new BattleUnitAttackedEvent(cleric.Id, warrior.Id, new UnitAttackDefinitionId("cleric")),
            new BattleUnitAttackHitEvent(cleric.Id, warrior.Id, new UnitAttackDefinitionId("cleric"), 0),
            new BattleUnitHealedEvent(warrior.Id, 80),
            new BattleUnitAttackedEvent(cleric.Id, ally.Id, new UnitAttackDefinitionId("cleric")),
            new BattleUnitAttackHitEvent(cleric.Id, ally.Id, new UnitAttackDefinitionId("cleric"), 0),
            new BattleUnitHealedEvent(ally.Id, 90),
            new BattleUnitAttackedEvent(cleric.Id, cleric.Id, new UnitAttackDefinitionId("cleric")),
            new BattleUnitAttackHitEvent(cleric.Id, cleric.Id, new UnitAttackDefinitionId("cleric"), 0),
            new BattleUnitHealedEvent(cleric.Id, 100),
            new BattleUnitTurnEndingEvent(cleric.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(ally.Id, 1),
            new BattleUnitExperienceGainedEvent(cleric.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { Hitpoints = 80, ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(ally with { Hitpoints = 90, ExperienceGained = 1 }, GridPosition.FrontTop, false)
                .Place(cleric with { Hitpoints = 100, ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void DefenderHealsSurvivorTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 20, damage: 5);
        UnitDefinition dwarfDefinition = TestHelpers.SimpleUnitDefinition("dwarf", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition priestDefinition = HealerDefinition("priest", maxHitpoints: 100, initiative: 40, heal: 30, UnitAttackReach.Any);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit dwarf = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, dwarfDefinition.Id, BattleSide.Defender, hitpoints: 50);
        BattleUnit priest = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, priestDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(dwarfDefinition)
            .WithDefinition(priestDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(dwarf, GridPosition.FrontBottom)
            .WithDefender(priest, GridPosition.BackBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(dwarf.Id, new BattleAttackUnitTarget(attacker.Id)), [
            new BattleUnitAttackEvent(dwarf.Id, new BattleAttackUnitTarget(attacker.Id), [new UnitAttackDefinitionId("dwarf")]),
            new BattleUnitAttackedEvent(dwarf.Id, attacker.Id, new UnitAttackDefinitionId("dwarf")),
            new BattleUnitAttackHitEvent(dwarf.Id, attacker.Id, new UnitAttackDefinitionId("dwarf"), 10),
            new BattleUnitDiedEvent(attacker.Id),
            new BattleUnitTurnEndingEvent(dwarf.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Recovery(1), [new BattleUnitTurn(priest.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnEndedEvent(dwarf.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(priest.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(priest.Id, new BattleAttackUnitTarget(dwarf.Id)), [
            new BattleUnitAttackEvent(priest.Id, new BattleAttackUnitTarget(dwarf.Id), [new UnitAttackDefinitionId("priest")]),
            new BattleUnitAttackedEvent(priest.Id, dwarf.Id, new UnitAttackDefinitionId("priest")),
            new BattleUnitAttackHitEvent(priest.Id, dwarf.Id, new UnitAttackDefinitionId("priest"), 0),
            new BattleUnitHealedEvent(dwarf.Id, 80),
            new BattleUnitTurnEndingEvent(priest.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(dwarf.Id, 1),
            new BattleUnitExperienceGainedEvent(priest.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(dwarf with { Hitpoints = 80, ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(priest with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(priest.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void RevivesDeadAllyTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition fallenDefinition = TestHelpers.SimpleUnitDefinition("fallen", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition reviverDefinition = SupportDefinition("reviver", maxHitpoints: 100, initiative: 40, UnitAttackType.Revive, heal: 40);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit fallen = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, fallenDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit reviver = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, reviverDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(fallenDefinition)
            .WithDefinition(reviverDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(fallen, GridPosition.FrontTop)
            .WithAttacker(reviver, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Recovery(1), [new BattleUnitTurn(reviver.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(reviver.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(reviver.Id, new BattleAttackUnitTarget(fallen.Id)), [
            new BattleUnitAttackEvent(reviver.Id, new BattleAttackUnitTarget(fallen.Id), [new UnitAttackDefinitionId("reviver")]),
            new BattleUnitAttackedEvent(reviver.Id, fallen.Id, new UnitAttackDefinitionId("reviver")),
            new BattleUnitAttackHitEvent(reviver.Id, fallen.Id, new UnitAttackDefinitionId("reviver"), 0),
            new BattleUnitRevivedEvent(fallen.Id, 50),
            new BattleUnitTurnEndingEvent(reviver.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(fallen.Id, 1),
            new BattleUnitExperienceGainedEvent(reviver.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(fallen with { Hitpoints = 50, IsRevived = true, ExperienceGained = 1 }, GridPosition.FrontTop, false)
                .Place(reviver with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(reviver.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void DamageOverTimeEffectTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition afflictedDefinition = TestHelpers.SimpleUnitDefinition("afflicted", maxHitpoints: 100, initiative: 30, damage: 5);
        UnitDefinition curerDefinition = HealerDefinition("curer", maxHitpoints: 100, initiative: 40, heal: 30, UnitAttackReach.Any);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit afflicted = new(new BattleUnitId(1), null, afflictedDefinition.Id, BattleSide.Attacker, 50, 0, 0, 1, [], [new BattlePoisonStatusEffect(1, 5)], null, [], null, false);
        BattleUnit curer = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, curerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(afflictedDefinition)
            .WithDefinition(curerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(afflicted, GridPosition.FrontTop)
            .WithAttacker(curer, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Recovery(1), [new BattleUnitTurn(curer.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(curer.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(curer.Id, new BattleAttackUnitTarget(afflicted.Id)), [
            new BattleUnitAttackEvent(curer.Id, new BattleAttackUnitTarget(afflicted.Id), [new UnitAttackDefinitionId("curer")]),
            new BattleUnitAttackedEvent(curer.Id, afflicted.Id, new UnitAttackDefinitionId("curer")),
            new BattleUnitAttackHitEvent(curer.Id, afflicted.Id, new UnitAttackDefinitionId("curer"), 0),
            new BattleUnitHealedEvent(afflicted.Id, 80),
            new BattleUnitTurnEndingEvent(curer.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(afflicted.Id, 1),
            new BattleUnitExperienceGainedEvent(curer.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(afflicted with { Hitpoints = 80, StatusEffects = [], ExperienceGained = 1 }, GridPosition.FrontTop, false)
                .Place(curer with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(curer.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void CuresFullHealthAfflictedAllyTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition afflictedDefinition = TestHelpers.SimpleUnitDefinition("afflicted", maxHitpoints: 100, initiative: 30, damage: 5);
        UnitDefinition curerDefinition = SupportDefinition("curer", maxHitpoints: 100, initiative: 40, UnitAttackType.Cure, heal: 0);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit afflicted = new(new BattleUnitId(1), null, afflictedDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [new BattleParalyzeStatusEffect(1)], null, [], null, false);
        BattleUnit curer = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, curerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(afflictedDefinition)
            .WithDefinition(curerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(afflicted, GridPosition.FrontTop)
            .WithAttacker(curer, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Recovery(1), [new BattleUnitTurn(curer.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(curer.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(curer.Id, new BattleAttackUnitTarget(afflicted.Id)), [
            new BattleUnitAttackEvent(curer.Id, new BattleAttackUnitTarget(afflicted.Id), [new UnitAttackDefinitionId("curer")]),
            new BattleUnitAttackedEvent(curer.Id, afflicted.Id, new UnitAttackDefinitionId("curer")),
            new BattleUnitAttackHitEvent(curer.Id, afflicted.Id, new UnitAttackDefinitionId("curer"), 0),
            new BattleUnitCuredEvent(afflicted.Id),
            new BattleUnitTurnEndingEvent(curer.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(afflicted.Id, 1),
            new BattleUnitExperienceGainedEvent(curer.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(afflicted with { StatusEffects = [], ExperienceGained = 1 }, GridPosition.FrontTop, false)
                .Place(curer with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(curer.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void NoCurableEffectTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition curerDefinition = SupportDefinition("curer", maxHitpoints: 100, initiative: 40, UnitAttackType.Cure, heal: 0);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);

        BattleUnit warrior = new(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [new BattleCureStatusEffect()], null, [], null, false);
        BattleUnit curer = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, curerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(curerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(curer, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(curer.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { StatusEffects = [], ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(curer with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(curer.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void NoDeadAllyTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition reviverDefinition = SupportDefinition("reviver", maxHitpoints: 100, initiative: 40, UnitAttackType.Revive, heal: 40);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit reviver = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, reviverDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(reviverDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(reviver, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(reviver.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(reviver with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(reviver.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void ZeroHealTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition clericDefinition = HealerDefinition("cleric", maxHitpoints: 100, initiative: 40, heal: 0, UnitAttackReach.Any);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(clericDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(cleric, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(cleric.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(cleric with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void ResurrectsUnitFreedFromSummonTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 100, initiative: 80, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 80, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 70, damage: 10);
        UnitDefinition resurrectorDefinition = SupportDefinition("resurrector", maxHitpoints: 100, initiative: 40, type: UnitAttackType.Revive, heal: 100);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 10, initiative: 45, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit resurrector = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, resurrectorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(4), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(warriorDefinition)
            .WithDefinition(resurrectorDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(summoner, GridPosition.BackBottom)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(resurrector, GridPosition.BackTop)
            .WithAttacker(ally, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleUnitId summonId = new(5);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));

        BattleUnit summon = new(summonId, null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.BackBottom, false)
                .Place(warrior, GridPosition.FrontBottom, false)
                .Place(resurrector, GridPosition.BackTop, false)
                .Place(ally, GridPosition.FrontCenter, false)
                .PlaceOverlay(summon, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(resurrector.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleUnitUnsummonedEvent(summonId),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Recovery(1), [new BattleUnitTurn(resurrector.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(resurrector.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(resurrector.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(resurrector.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("resurrector")]),
            new BattleUnitAttackedEvent(resurrector.Id, ally.Id, new UnitAttackDefinitionId("resurrector")),
            new BattleUnitAttackHitEvent(resurrector.Id, ally.Id, new UnitAttackDefinitionId("resurrector"), 0),
            new BattleUnitRevivedEvent(ally.Id, 5),
            new BattleUnitTurnEndingEvent(resurrector.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(ally.Id, 1),
            new BattleUnitExperienceGainedEvent(summoner.Id, 1),
            new BattleUnitExperienceGainedEvent(resurrector.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Place(warrior with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(resurrector with { ExperienceGained = 1 }, GridPosition.BackTop, false)
                .Place(ally with { Hitpoints = 5, ExperienceGained = 1, IsRevived = true }, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(resurrector.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void HealPotionTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition leaderDefinition = HealerDefinition("leader", maxHitpoints: 100, initiative: 40, heal: 30, UnitAttackReach.Any) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);
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

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(leader, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Recovery(1), [new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle1, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleItemUsedEvent(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, leader.Id, new BattleAttackUnitTarget(warrior.Id), RemainingCharges: null, Consumed: true),
            new BattleUnitHealedEvent(warrior.Id, 100),
            new BattleUnitTurnEndingEvent(leader.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(leader.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { Hitpoints = 100, ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(leader with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true)],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SummonTalismanTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition leaderDefinition = HealerDefinition("leader", maxHitpoints: 100, initiative: 40, heal: 30, UnitAttackReach.Any) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitAttackDefinition summonAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summon"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Type = UnitAttackType.Summon,
            SummonUnitIds = [summonDefinition.Id]
        };
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

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(summonDefinition)
            .WithDefinition(summonAttack)
            .WithDefinition(talisman)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(leader, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle2, talisman.Id, Charges: 5, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Recovery(1), [new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommandError(
            new BattleUseItemCommand(leader.Id, LeaderEquipmentSlot.Battle2, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop)),
            BattleCommandError.ItemNotUsable);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior, GridPosition.FrontBottom, false)
                .Place(leader, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle2, talisman.Id, Charges: 5, Consumed: false)],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void GetItemInfoTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition leaderDefinition = HealerDefinition("leader", maxHitpoints: 100, initiative: 40, heal: 30, UnitAttackReach.Any) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);
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
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitAttackDefinition summonAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summon"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Type = UnitAttackType.Summon,
            SummonUnitIds = [summonDefinition.Id]
        };
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

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(potion)
            .WithDefinition(summonDefinition)
            .WithDefinition(summonAttack)
            .WithDefinition(talisman)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(leader, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .WithItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle2, talisman.Id, Charges: 5, Consumed: false))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, defender.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Recovery(1), [new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal))
        ]);

        BattleGetItemInfoCommand.Result result = engine.CheckCommand(new BattleGetItemInfoCommand(leader.Id), []);

        Assert.That(result, Is.EqualTo(new BattleGetItemInfoCommand.Result(
        [
            new BattleGetItemInfoCommand.SlotInfo(LeaderEquipmentSlot.Battle1,
            [
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(warrior.Id), [new BattleAttackUnitTarget(warrior.Id)], null),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(leader.Id), [], BattleCommandError.TargetNotHealable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(defender.Id), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontBottom), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontBottom), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.ItemTargetInvalid),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.ItemTargetInvalid)
            ]),
            new BattleGetItemInfoCommand.SlotInfo(LeaderEquipmentSlot.Battle2,
            [
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(warrior.Id), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(leader.Id), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackUnitTarget(defender.Id), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontBottom), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackBottom), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackCenter), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.BackTop), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontBottom), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontTop), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackBottom), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackCenter), [], BattleCommandError.ItemNotUsable),
                new BattleGetItemInfoCommand.ItemInfo(new BattleAttackTileTarget(BattleSide.Defender, GridPosition.BackTop), [], BattleCommandError.ItemNotUsable)
            ])
        ])));
    }

    [Test]
    public void SummonTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition clericDefinition = HealerDefinition("cleric", maxHitpoints: 100, initiative: 40, heal: 30, UnitAttackReach.Any);
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 20, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 0);
        BattleUnit imp = new(new BattleUnitId(3), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], cleric.Id, [], null, false);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(warriorDefinition)
            .AddUnit(clericDefinition)
            .AddUnit(impDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        BattleState state = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior, GridPosition.FrontBottom, false)
                .Place(cleric, GridPosition.BackBottom, false)
                .PlaceOverlay(imp, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);
        BattleEngine engine = new(
            new EngineContext(new TestSeedRandomProvider(), definitions, new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition), GameScenarioProvider.Empty, globals),
            state,
            NullBattleUnitUpgradeResolver.Instance);

        engine.CheckCommand(new BattleAttackCommand(cleric.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(cleric.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("cleric")]),
            new BattleUnitAttackedEvent(cleric.Id, warrior.Id, new UnitAttackDefinitionId("cleric")),
            new BattleUnitAttackHitEvent(cleric.Id, warrior.Id, new UnitAttackDefinitionId("cleric"), 0),
            new BattleUnitHealedEvent(warrior.Id, 80),
            new BattleUnitTurnEndingEvent(cleric.Id),
            new BattleUnitUnsummonedEvent(imp.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(cleric.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { Hitpoints = 80, ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(cleric with { ExperienceGained = 1 }, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(cleric.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Recovery(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }
}
