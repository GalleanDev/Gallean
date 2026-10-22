namespace Gallean.Engine.Tests;

public sealed class BattleEngineDamageOverTimeAttackTests
{
    private static UnitDefinition DamageOverTimeAttacker(string id, UnitAttackType type, UnitAttackSource source, int damage, bool isInfinite, int accuracy)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage, accuracy, reach: UnitAttackReach.Any) with
            {
                Type = type,
                Source = source,
                Level = 1,
                IsInfinite = isInfinite
            }
        };
    }

    [Test]
    public void WeakerPoisonTest()
    {
        UnitDefinition attackerDefinition = DamageOverTimeAttacker("attacker", UnitAttackType.Poison, UnitAttackSource.Death, damage: 3, isInfinite: false, accuracy: 100);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = new(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, 10, 0, 0, 1, [], [new BattlePoisonStatusEffect(2, 5)], null, [], null, false);

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
    public void RetreatingTargetTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 60, damage: 5);
        UnitDefinition poisonerDefinition = DamageOverTimeAttacker("poisoner", UnitAttackType.Poison, UnitAttackSource.Death, damage: 3, isInfinite: false, accuracy: 100);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit poisoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, poisonerDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(poisonerDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithDefender(poisoner, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleRetreatCommand(warrior.Id));

        engine.CheckCommandError(
            new BattleAttackCommand(poisoner.Id, new BattleAttackUnitTarget(warrior.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void PoisonTicksTest()
    {
        UnitDefinition attackerDefinition = DamageOverTimeAttacker("attacker", UnitAttackType.Poison, UnitAttackSource.Death, damage: 3, isInfinite: false, accuracy: 100);
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
            new BattleUnitEffectAppliedEvent(attacker.Id, defender.Id, new BattlePoisonStatusEffect(1, 3)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(1, 3), 3, 7),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattlePoisonStatusEffect(0, 3))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 7 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void FrostbiteTicksTest()
    {
        UnitDefinition attackerDefinition = DamageOverTimeAttacker("attacker", UnitAttackType.Frostbite, UnitAttackSource.Water, damage: 4, isInfinite: false, accuracy: 100);
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
            new BattleUnitEffectAppliedEvent(attacker.Id, defender.Id, new BattleFrostbiteStatusEffect(1, 4)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattleFrostbiteStatusEffect(1, 4), 4, 6),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattleFrostbiteStatusEffect(0, 4))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 6 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void BlisterTicksTest()
    {
        UnitDefinition attackerDefinition = DamageOverTimeAttacker("attacker", UnitAttackType.Blister, UnitAttackSource.Fire, damage: 2, isInfinite: false, accuracy: 100);
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
            new BattleUnitEffectAppliedEvent(attacker.Id, defender.Id, new BattleBlisterStatusEffect(1, 2)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattleBlisterStatusEffect(1, 2), 2, 8),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattleBlisterStatusEffect(0, 2))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 8 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void MissTest()
    {
        UnitDefinition attackerDefinition = DamageOverTimeAttacker("attacker", UnitAttackType.Poison, UnitAttackSource.Death, damage: 3, isInfinite: false, accuracy: 0);
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
            new BattleUnitAttackMissedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
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
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void InfiniteRandomDurationTest()
    {
        UnitDefinition attackerDefinition = DamageOverTimeAttacker("attacker", UnitAttackType.Poison, UnitAttackSource.Death, damage: 2, isInfinite: true, accuracy: 100);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

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
            new BattleUnitEffectAppliedEvent(attacker.Id, defender.Id, new BattlePoisonStatusEffect(3, 2)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(3, 2), 2, 18),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 18, StatusEffects = [new BattlePoisonStatusEffect(2, 2)] }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));

        _ = engine.DispatchCommand(new BattleSkipCommand(defender.Id));

        engine.CheckCommand(new BattleSkipCommand(attacker.Id), [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(2, 2), 2, 16),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 16, StatusEffects = [new BattlePoisonStatusEffect(1, 2)] }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));

        _ = engine.DispatchCommand(new BattleSkipCommand(defender.Id));

        engine.CheckCommand(new BattleSkipCommand(attacker.Id), [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(1, 2), 2, 14),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattlePoisonStatusEffect(0, 2))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 14 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(3)
            }));

        _ = engine.DispatchCommand(new BattleSkipCommand(defender.Id));

        engine.CheckCommand(new BattleSkipCommand(attacker.Id), [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 14 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(4)
            }));
    }

    [Test]
    public void IndependentEffectsTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = new(
            new BattleUnitId(1),
            null,
            defenderDefinition.Id,
            BattleSide.Defender,
            20,
            0,
            0,
            1,
            [],
            [new BattlePoisonStatusEffect(2, 3), new BattleFrostbiteStatusEffect(2, 2)],
            null,
            [],
            Name: null,
            IsTypeLocked: false);

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
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(2, 3), 3, 17),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattleFrostbiteStatusEffect(2, 2), 2, 15),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(
                        defender with
                        {
                            Hitpoints = 15,
                            StatusEffects = [new BattlePoisonStatusEffect(1, 3), new BattleFrostbiteStatusEffect(1, 2)]
                        },
                        GridPosition.FrontBottom,
                        false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void KillsTargetTest()
    {
        UnitDefinition attackerDefinition = DamageOverTimeAttacker("attacker", UnitAttackType.Poison, UnitAttackSource.Death, damage: 5, isInfinite: false, accuracy: 100);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 5, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 5);

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
            new BattleUnitEffectAppliedEvent(attacker.Id, defender.Id, new BattlePoisonStatusEffect(1, 5)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(1, 5), 5, 0),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(attacker.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1),
                Outcome = new BattleOutcome(BattleSide.Attacker)
            }));
    }

    [Test]
    public void AttacksTwiceTicksOnceTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5) with
        {
            AttacksTwice = true
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = new(
            new BattleUnitId(1),
            null,
            defenderDefinition.Id,
            BattleSide.Defender,
            20,
            0,
            0,
            1,
            [],
            [new BattlePoisonStatusEffect(3, 2)],
            null,
            [],
            Name: null,
            IsTypeLocked: false);

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
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(3, 2), 2, 18),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id)
        ]);

        engine.CheckCommand(new BattleAttackCommand(defender.Id, new BattleAttackUnitTarget(attacker.Id)), [
            new BattleUnitAttackEvent(defender.Id, new BattleAttackUnitTarget(attacker.Id), [new UnitAttackDefinitionId("defender")]),
            new BattleUnitAttackedEvent(defender.Id, attacker.Id, new UnitAttackDefinitionId("defender")),
            new BattleUnitAttackHitEvent(defender.Id, attacker.Id, new UnitAttackDefinitionId("defender"), 5),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.SecondAttack))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { Hitpoints = 95 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(
                        defender with
                        {
                            Hitpoints = 18,
                            StatusEffects = [new BattlePoisonStatusEffect(2, 2)]
                        },
                        GridPosition.FrontBottom,
                        false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.SecondAttack),
                TurnQueue = [],
                CurrentRound = BattleRound.Normal(1)
            }));

        engine.CheckCommand(new BattleAttackCommand(defender.Id, new BattleAttackUnitTarget(attacker.Id)), [
            new BattleUnitAttackEvent(defender.Id, new BattleAttackUnitTarget(attacker.Id), [new UnitAttackDefinitionId("defender")]),
            new BattleUnitAttackedEvent(defender.Id, attacker.Id, new UnitAttackDefinitionId("defender")),
            new BattleUnitAttackHitEvent(defender.Id, attacker.Id, new UnitAttackDefinitionId("defender"), 5),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleSkipCommand(attacker.Id), [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(2, 2), 2, 16),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { Hitpoints = 90 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(
                        defender with
                        {
                            Hitpoints = 16,
                            StatusEffects = [new BattlePoisonStatusEffect(1, 2)]
                        },
                        GridPosition.FrontBottom,
                        false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                TurnQueue = [],
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void WaitTicksOnceTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = new(
            new BattleUnitId(1),
            null,
            defenderDefinition.Id,
            BattleSide.Defender,
            20,
            0,
            0,
            1,
            [],
            [new BattlePoisonStatusEffect(3, 2)],
            null,
            [],
            Name: null,
            IsTypeLocked: false);

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
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(3, 2), 2, 18),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id)
        ]);

        engine.CheckCommand(new BattleWaitCommand(defender.Id), [
            new BattleUnitWaitedEvent(defender.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Resumed))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(
                        defender with
                        {
                            Hitpoints = 18,
                            StatusEffects = [new BattlePoisonStatusEffect(2, 2)]
                        },
                        GridPosition.FrontBottom,
                        false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Resumed),
                TurnQueue = [],
                WaitingQueue = [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Resumed)],
                CurrentRound = BattleRound.Normal(1)
            }));

        engine.CheckCommand(new BattleSkipCommand(defender.Id), [
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleSkipCommand(attacker.Id), [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(2, 2), 2, 16),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(
                        defender with
                        {
                            Hitpoints = 16,
                            StatusEffects = [new BattlePoisonStatusEffect(1, 2)]
                        },
                        GridPosition.FrontBottom,
                        false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                TurnQueue = [],
                WaitingQueue = [],
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void AutoResolveTest()
    {
        UnitDefinition poisonerDefinition = TestHelpers.SimpleUnitDefinition("poisoner", maxHitpoints: 100, initiative: 60, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 60, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Poison,
                Source = UnitAttackSource.Death,
                Level = 1
            }
        };
        UnitDefinition victimDefinition = TestHelpers.SimpleUnitDefinition("victim", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit poisoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, poisonerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit victim = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, victimDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(poisonerDefinition)
            .WithDefinition(victimDefinition)
            .WithAttacker(poisoner, GridPosition.FrontBottom)
            .WithDefender(victim, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAutoResolveCommand());

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(poisoner with { Hitpoints = 85, ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(victim with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(victim.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(4),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void DamageModifierTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("poisoner", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("poisoner-poison"), initiative: 50, damage: 3, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Poison,
                Source = UnitAttackSource.Death,
                Level = 1
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        UnitModifierDefinition damageBonus = TestHelpers.SimpleModifier("damage") with
        {
            Damage = new UnitStatModifier(0, 100)
        };

        BattleUnit attacker = new(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, 10, 0, 0, 1, [UnitModifier.Permanent(damageBonus.Id)], [], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(damageBonus)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("poisoner-poison")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("poisoner-poison")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("poisoner-poison"), 0),
            new BattleUnitEffectAppliedEvent(attacker.Id, defender.Id, new BattlePoisonStatusEffect(1, 3)),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(defender.Id, new BattlePoisonStatusEffect(1, 3), 3, 7),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id),
            new BattleUnitEffectExpiredEvent(defender.Id, new BattlePoisonStatusEffect(0, 3))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 7 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }
}
