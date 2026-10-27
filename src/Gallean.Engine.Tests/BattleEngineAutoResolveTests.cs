using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class BattleEngineAutoResolveTests
{
    [Test]
    public void CompletesTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("unit0", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("unit1", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAutoResolveCommand(),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("unit0")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("unit0")),
                new BattleUnitAttackHitEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("unit0"), 5),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)),
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(0)), [new UnitAttackDefinitionId("unit1")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(0), new UnitAttackDefinitionId("unit1")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(0), new UnitAttackDefinitionId("unit1"), 5),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1)),
                new BattleRoundEndedEvent(),
                new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal), new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)]),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal)),
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("unit0")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("unit0")),
                new BattleUnitAttackHitEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("unit0"), 5),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)),
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(0)), [new UnitAttackDefinitionId("unit1")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(0), new UnitAttackDefinitionId("unit1")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(0), new UnitAttackDefinitionId("unit1"), 5),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1)),
                new BattleRoundEndedEvent(),
                new BattleRoundStartedEvent(BattleRound.Normal(3), [new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal), new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)]),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal)),
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("unit0")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("unit0")),
                new BattleUnitAttackHitEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("unit0"), 5),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(0)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)),
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(0)), [new UnitAttackDefinitionId("unit1")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(0), new UnitAttackDefinitionId("unit1")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(0), new UnitAttackDefinitionId("unit1"), 5),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1)),
                new BattleRoundEndedEvent(),
                new BattleRoundStartedEvent(BattleRound.Normal(4), [new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal), new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)]),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal)),
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("unit0")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("unit0")),
                new BattleUnitAttackHitEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("unit0"), 5),
                new BattleUnitDiedEvent(UnitId: new BattleUnitId(1)),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleRoundEndedEvent(),
                new BattleEndedEvent(WinningSide: BattleSide.Attacker),
                new BattleUnitExperienceGainedEvent(new BattleUnitId(0), 1)
            ]
        );

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker with { Hitpoints = 5, ExperienceGained = 1 }, new GridPosition(0, 0), false)
            .Build();

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Hitpoints = 0 }, new GridPosition(0, 0), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = expectedAttackerGrid,
                DefenderGrid = expectedDefenderGrid,
                CurrentUnitTurn = new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(4),
                Outcome = new BattleOutcome(BattleSide.Attacker)
            }));
    }

    [Test]
    public void FinishedTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 50, damage: 25);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, scoutDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(scoutDefinition)
            .WithAttacker(warrior, new GridPosition(0, 0))
            .WithDefender(scout, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1))),
            [
                new BattleUnitAttackEvent(new BattleUnitId(0), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("warrior")]),
                new BattleUnitAttackedEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("warrior")),
                new BattleUnitAttackHitEvent(new BattleUnitId(0), new BattleUnitId(1), new UnitAttackDefinitionId("warrior"), 25),
                new BattleUnitDiedEvent(UnitId: new BattleUnitId(1)),
                new BattleUnitTurnEndingEvent(new BattleUnitId(0)),
                new BattleRoundEndedEvent(),
                new BattleEndedEvent(WinningSide: BattleSide.Attacker),
                new BattleUnitExperienceGainedEvent(new BattleUnitId(0), 1)
            ]
        );

        engine.CheckCommandError(new BattleAutoResolveCommand(), BattleCommandError.NoActiveBattle);

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(warrior with { ExperienceGained = 1 }, new GridPosition(0, 0), false)
            .Build();

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(scout with { Hitpoints = 0 }, new GridPosition(0, 0), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = expectedAttackerGrid,
                DefenderGrid = expectedDefenderGrid,
                CurrentUnitTurn = new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1),
                Outcome = new BattleOutcome(BattleSide.Attacker)
            }));
    }

    [Test]
    public void EmptyDefenderTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("unit0", maxHitpoints: 20, initiative: 50, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .Build();

        BattleCommandResult result = engine.DispatchCommand(new BattleAutoResolveCommand());

        Assert.That(result.Error, Is.Null);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker with { ExperienceGained = 1 }, new GridPosition(0, 0), false)
                .Build(),
            DefenderGrid: UnitGrid<BattleUnit>.Empty,
            CurrentUnitTurn: new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void InconclusiveTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("unit0", maxHitpoints: 20, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit0"), initiative: 50, damage: 0, accuracy: 0, reach: UnitAttackReach.Any)
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("unit1", maxHitpoints: 20, initiative: 40, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit1"), initiative: 40, damage: 0, accuracy: 0, reach: UnitAttackReach.Any)
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        BattleCommandResult result = engine.DispatchCommand(new BattleAutoResolveCommand());

        Assert.That(result.Error, Is.Null);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker with { StatusEffects = [new BattleRetreatedStatusEffect()] }, new GridPosition(0, 0), false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { ExperienceGained = 1 }, new GridPosition(0, 0), false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(18),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void HealLockStalemateTest()
    {
        UnitDefinition dealerDefinition = TestHelpers.SimpleUnitDefinition("dealer", maxHitpoints: 20, initiative: 60, damage: 3);

        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 20, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Heal = 100
            }
        };

        BattleUnit attackerDealer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, dealerDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit attackerHealer = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, healerDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit defenderDealer = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, dealerDefinition.Id, BattleSide.Defender, hitpoints: 20);
        BattleUnit defenderHealer = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, healerDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(dealerDefinition)
            .WithDefinition(healerDefinition)
            .WithAttacker(attackerDealer, new GridPosition(0, 0))
            .WithAttacker(attackerHealer, new GridPosition(1, 0))
            .WithDefender(defenderDealer, new GridPosition(0, 0))
            .WithDefender(defenderHealer, new GridPosition(1, 0))
            .Build();

        BattleCommandResult result = engine.DispatchCommand(new BattleAutoResolveCommand());

        Assert.That(result.Error, Is.Null);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attackerDealer with { Hitpoints = 17, StatusEffects = [new BattleRetreatedStatusEffect()] }, new GridPosition(0, 0), false)
                .Place(attackerHealer with { Hitpoints = 17, StatusEffects = [new BattleRetreatedStatusEffect()] }, new GridPosition(1, 0), false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defenderDealer with { ExperienceGained = 1 }, new GridPosition(0, 0), false)
                .Place(defenderHealer with { ExperienceGained = 1 }, new GridPosition(1, 0), false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(attackerHealer.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defenderHealer.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(18),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void TwiceAttackerTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("twice", maxHitpoints: 20, initiative: 50, damage: 5) with
        {
            AttacksTwice = true
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        BattleCommandResult result = engine.DispatchCommand(new BattleAutoResolveCommand());

        Assert.That(result.Error, Is.Null);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker with { Hitpoints = 15, ExperienceGained = 1 }, new GridPosition(0, 0), false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, new GridPosition(0, 0), false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(attacker.Id, BattleUnitTurnType.SecondAttack),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(2),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SupportOnlySidesTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("healer0", maxHitpoints: 20, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer0"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Heal = 5
            }
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("healer1", maxHitpoints: 20, initiative: 40, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer1"), initiative: 40, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Heal = 5
            }
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        BattleCommandResult result = engine.DispatchCommand(new BattleAutoResolveCommand());

        Assert.That(result.Error, Is.Null);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker with { StatusEffects = [new BattleRetreatedStatusEffect()] }, new GridPosition(0, 0), false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { ExperienceGained = 1 }, new GridPosition(0, 0), false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(18),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void GiveAttackUnitTest()
    {
        UnitDefinition alchemistDefinition = TestHelpers.SimpleUnitDefinition("alchemist", maxHitpoints: 20, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("alchemist"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.GiveAdditionalAttack
            }
        };

        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 20, initiative: 30, damage: 8);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 20, initiative: 40, damage: 8);

        BattleUnit alchemist = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, alchemistDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(alchemistDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(alchemist, new GridPosition(0, 0))
            .WithAttacker(ally, new GridPosition(1, 0))
            .WithDefender(enemy, new GridPosition(0, 0))
            .Build();

        BattleCommandResult result = engine.DispatchCommand(new BattleAutoResolveCommand());

        Assert.That(result.Error, Is.Null);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(alchemist with { Hitpoints = 12, ExperienceGained = 1 }, new GridPosition(0, 0), false)
                .Place(ally with { ExperienceGained = 1 }, new GridPosition(1, 0), false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(enemy with { Hitpoints = 0 }, new GridPosition(0, 0), false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(ally.Id, BattleUnitTurnType.GrantedAttack),
            TurnQueue: [new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(2),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void HealerTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 60, damage: 8);

        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 20, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Heal = 3
            }
        };

        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 20, initiative: 40, damage: 8);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, healerDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(healerDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(warrior, new GridPosition(0, 0))
            .WithAttacker(healer, new GridPosition(1, 0))
            .WithDefender(enemy, new GridPosition(0, 0))
            .Build();

        BattleCommandResult result = engine.DispatchCommand(new BattleAutoResolveCommand());

        Assert.That(result.Error, Is.Null);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { Hitpoints = 10, ExperienceGained = 1 }, new GridPosition(0, 0), false)
                .Place(healer with { ExperienceGained = 1 }, new GridPosition(1, 0), false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(enemy with { Hitpoints = 0 }, new GridPosition(0, 0), false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(healer.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Recovery(3),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void StalemateParalyzingDefenderTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 20, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 0, accuracy: 0, reach: UnitAttackReach.Any)
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("defender"), initiative: 40, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Mind,
                Level = 1
            }
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        BattleCommandResult result = engine.DispatchCommand(new BattleAutoResolveCommand());

        Assert.That(result.Error, Is.Null);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker with { StatusEffects = [new BattleRetreatedStatusEffect()] }, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(34),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void StalemateLateRoundTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("unit0", maxHitpoints: 20, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit0"), initiative: 50, damage: 0, accuracy: 0, reach: UnitAttackReach.Any)
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("unit1", maxHitpoints: 20, initiative: 40, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit1"), initiative: 40, damage: 0, accuracy: 0, reach: UnitAttackReach.Any)
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .WithRoundNumber(30)
            .Build();

        BattleCommandResult result = engine.DispatchCommand(new BattleAutoResolveCommand());

        Assert.That(result.Error, Is.Null);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker with { StatusEffects = [new BattleRetreatedStatusEffect()] }, new GridPosition(0, 0), false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { ExperienceGained = 1 }, new GridPosition(0, 0), false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(47),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void TransformSelfUnitTest()
    {
        UnitDefinition beastDefinition = TestHelpers.SimpleUnitDefinition("beast", maxHitpoints: 40, initiative: 50, damage: 20);

        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("unit0", maxHitpoints: 20, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit0"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.TransformSelf,
                SmallTransformUnitId = beastDefinition.Id
            }
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("unit1", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(beastDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        BattleCommandResult result = engine.DispatchCommand(new BattleAutoResolveCommand());

        Assert.That(result.Error, Is.Null);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker with { ExperienceGained = 1 }, new GridPosition(0, 0), false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 0 }, new GridPosition(0, 0), false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SummonTest()
    {
        UnitWard originalWard = new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Once);
        UnitDefinition originalDefinition = TestHelpers.SimpleUnitDefinition("original", maxHitpoints: 100, initiative: 30, damage: 1) with
        {
            Wards = [originalWard]
        };
        UnitDefinition transformedDefinition = TestHelpers.SimpleUnitDefinition("transformed", maxHitpoints: 50, initiative: 30, damage: 1);
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit transformed = new(
            new BattleUnitId(1),
            null,
            transformedDefinition.Id,
            BattleSide.Attacker,
            0,
            0,
            0,
            1,
            [],
            [new BattleTransformStatusEffect(BattleTransformType.Transform, originalDefinition.Id, 1, [originalWard], null)],
            null,
            [],
            Name: null,
            IsTypeLocked: false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 0);
        BattleUnit imp = new(new BattleUnitId(3), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summoner.Id, [], null, false);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(originalDefinition)
            .AddUnit(transformedDefinition)
            .AddUnit(impDefinition)
            .AddUnit(summonerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        BattleState state = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner, GridPosition.FrontBottom, false)
                .Place(transformed, GridPosition.FrontCenter, false)
                .PlaceOverlay(imp, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: default,
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);
        BattleEngine engine = new(
            new EngineContext(new TestSeedRandomProvider(), definitions, new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition), GameScenarioProvider.Empty, globals),
            state,
            NullBattleUnitUpgradeResolver.Instance);

        _ = engine.DispatchCommand(new BattleStartCommand());

        engine.CheckCommand(new BattleAutoResolveCommand(),
        [
            new BattleUnitUnsummonedEvent(imp.Id),
            new BattleRoundEndedEvent(),
            new BattleUnitTransformRestoredEvent(transformed.Id, BattleTransformType.Transform, originalDefinition.Id, 0, 100),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(summoner.Id, 1)
        ]);

        BattleUnit restored = transformed with
        {
            DefinitionId = originalDefinition.Id,
            StatusEffects = [],
            Wards = [originalWard]
        };

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(summoner with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(restored, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }
}
