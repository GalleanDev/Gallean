namespace Gallean.Engine.Tests;

public sealed class BattleEngineDisableAttackTests
{
    [Test]
    public void ParalyzeTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Mind,
                Level = 1
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
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
    public void AlreadyDisabledTargetTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Mind,
                Level = 1
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = new(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, 10, 0, 0, 1, [], [new BattlePetrifyStatusEffect(2)], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void PetrifyTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Petrify,
                Source = UnitAttackSource.Mind,
                Level = 1
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 0),
            new BattleUnitEffectAppliedEvent(attacker.Id, defender.Id, new BattlePetrifyStatusEffect(1)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattlePetrifyStatusEffect(0)),
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
    public void InfiniteDurationTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Mind,
                Level = 1,
                IsInfinite = true
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 3, 1, 1, 3))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 0),
            new BattleUnitEffectAppliedEvent(attacker.Id, defender.Id, new BattleParalyzeStatusEffect(3)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitTurnSkippedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        UnitGrid<BattleUnit> expectedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = expectedAttackerGrid,
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { StatusEffects = [new BattleParalyzeStatusEffect(2)] }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));

        engine.CheckCommand(new BattleSkipCommand(attacker.Id), [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitTurnSkippedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(3), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = expectedAttackerGrid,
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { StatusEffects = [new BattleParalyzeStatusEffect(1)] }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(3)
            }));

        engine.CheckCommand(new BattleSkipCommand(attacker.Id), [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattleParalyzeStatusEffect(0)),
            new BattleUnitTurnSkippedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(4), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = expectedAttackerGrid,
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(4)
            }));

        engine.CheckCommand(new BattleSkipCommand(attacker.Id), [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = expectedAttackerGrid,
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(4)
            }));
    }

    [Test]
    public void AttacksTwiceDurationTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            AttacksTwice = true
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = new(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, 10, 0, 0, 1, [], [new BattleParalyzeStatusEffect(2)], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleSkipCommand(attacker.Id), [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
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
                    .Place(defender with { StatusEffects = [new BattleParalyzeStatusEffect(1)] }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void AutoResolveTest()
    {
        UnitDefinition paralyzerDefinition = TestHelpers.SimpleUnitDefinition("paralyzer", maxHitpoints: 30, initiative: 60, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Paralyze,
                Source = UnitAttackSource.Mind,
                Level = 1
            }
        };
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 30, initiative: 50, damage: 10);
        UnitDefinition victimDefinition = TestHelpers.SimpleUnitDefinition("victim", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit paralyzer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, paralyzerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit victim = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, victimDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(paralyzerDefinition)
            .WithDefinition(warriorDefinition)
            .WithDefinition(victimDefinition)
            .WithAttacker(paralyzer, GridPosition.FrontBottom)
            .WithAttacker(warrior, GridPosition.FrontTop)
            .WithDefender(victim, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAutoResolveCommand());

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(paralyzer with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(warrior with { ExperienceGained = 1 }, GridPosition.FrontTop, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(victim with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(victim.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(2),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void BattleEndTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 30, initiative: 50, damage: 10);
        UnitDefinition paralyzedDefinition = TestHelpers.SimpleUnitDefinition("paralyzed", maxHitpoints: 20, initiative: 40, damage: 5);
        UnitDefinition petrifiedDefinition = TestHelpers.SimpleUnitDefinition("petrified", maxHitpoints: 20, initiative: 35, damage: 5);
        UnitDefinition victimDefinition = TestHelpers.SimpleUnitDefinition("victim", maxHitpoints: 10, initiative: 30, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit paralyzed = new(new BattleUnitId(1), null, paralyzedDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [new BattleParalyzeStatusEffect(2)], null, [], null, false);
        BattleUnit petrified = new(new BattleUnitId(2), null, petrifiedDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [new BattlePetrifyStatusEffect(2)], null, [], null, false);
        BattleUnit victim = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, victimDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(paralyzedDefinition)
            .WithDefinition(petrifiedDefinition)
            .WithDefinition(victimDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(paralyzed, GridPosition.FrontTop)
            .WithAttacker(petrified, GridPosition.FrontCenter)
            .WithDefender(victim, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(victim.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(victim.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, victim.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, victim.Id, new UnitAttackDefinitionId("warrior"), 10),
            new BattleUnitDiedEvent(victim.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1),
            new BattleUnitExperienceGainedEvent(petrified.Id, 1),
            new BattleUnitExperienceGainedEvent(paralyzed.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(warrior with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Place(paralyzed with { ExperienceGained = 1, StatusEffects = [] }, GridPosition.FrontTop, false)
                .Place(petrified with { ExperienceGained = 1, StatusEffects = [] }, GridPosition.FrontCenter, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(victim with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(paralyzed.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(petrified.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(victim.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }
}
