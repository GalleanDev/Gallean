namespace Gallean.Engine.Tests;

public sealed class BattleEngineRetreatTests
{
    [Test]
    public void SkipsTurnTest()
    {
        UnitDefinition retreatingDefinition = TestHelpers.SimpleUnitDefinition("retreating", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 30, damage: 20);

        BattleUnit retreating = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, retreatingDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(retreatingDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(retreating, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleRetreatCommand(retreating.Id), [
            new BattleUnitRetreatingEvent(retreating.Id),
            new BattleUnitTurnEndingEvent(retreating.Id),
            new BattleUnitTurnEndedEvent(retreating.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(retreating with { StatusEffects = [new BattleRetreatStatusEffect()] })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal),
                TurnQueue = []
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void EscapeOnNextTurnTest()
    {
        UnitDefinition retreatingDefinition = TestHelpers.SimpleUnitDefinition("retreating", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 30, damage: 20);

        BattleUnit retreating = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, retreatingDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(retreatingDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(retreating, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleRetreatCommand(retreating.Id));

        engine.CheckCommand(new BattleSkipCommand(enemy.Id), [
            new BattleUnitTurnSkippedEvent(enemy.Id),
            new BattleUnitTurnEndingEvent(enemy.Id),
            new BattleUnitTurnEndedEvent(enemy.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal)),
            new BattleUnitRetreatedEvent(retreating.Id),
            new BattleUnitTurnEndingEvent(retreating.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(enemy.Id, 1)
        ]);

        BattleState expected = engine.State
                .WithUnit(retreating with { StatusEffects = [new BattleRetreatedStatusEffect()] })
                .WithUnit(enemy with { ExperienceGained = 1 })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(2),
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void PoisonTicksBeforeEscapeTest()
    {
        UnitDefinition retreatingDefinition = TestHelpers.SimpleUnitDefinition("retreating", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 30, damage: 20);

        BattleUnit retreating = new(new BattleUnitId(0), null, retreatingDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [new BattlePoisonStatusEffect(3, 2)], null, [], null, false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(retreatingDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(retreating, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleRetreatCommand(retreating.Id));

        engine.CheckCommand(new BattleSkipCommand(enemy.Id), [
            new BattleUnitTurnSkippedEvent(enemy.Id),
            new BattleUnitTurnEndingEvent(enemy.Id),
            new BattleUnitTurnEndedEvent(enemy.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(
                BattleRound.Normal(2),
                [new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(retreating.Id, new BattlePoisonStatusEffect(2, 2), 2, 96),
            new BattleUnitEffectDurationDecreasedEvent(retreating.Id),
            new BattleUnitRetreatedEvent(retreating.Id),
            new BattleUnitTurnEndingEvent(retreating.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(enemy.Id, 1)
        ]);

        BattleState expected = engine.State
                .WithUnit(retreating with { Hitpoints = 96, StatusEffects = [new BattleRetreatedStatusEffect()] })
                .WithUnit(enemy with { ExperienceGained = 1 })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(2),
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void PoisonKillsBeforeEscapingTest()
    {
        UnitDefinition retreatingDefinition = TestHelpers.SimpleUnitDefinition("retreating", maxHitpoints: 100, initiative: 30, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 50, damage: 20);

        BattleUnit retreating = new(new BattleUnitId(0), null, retreatingDefinition.Id, BattleSide.Attacker, 2, 0, 0, 1, [], [new BattleRetreatStatusEffect(), new BattlePoisonStatusEffect(3, 2)], null, [], null, false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(retreatingDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(retreating, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleSkipCommand(enemy.Id), [
            new BattleUnitTurnSkippedEvent(enemy.Id),
            new BattleUnitTurnEndingEvent(enemy.Id),
            new BattleUnitTurnEndedEvent(enemy.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal)),
            new BattleUnitDamagedByEffectEvent(retreating.Id, new BattlePoisonStatusEffect(3, 2), 2, 0),
            new BattleUnitDiedEvent(retreating.Id),
            new BattleUnitTurnEndingEvent(retreating.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(enemy.Id, 1)
        ]);

        BattleState expected = engine.State
                .WithUnit(retreating with { Hitpoints = 0, StatusEffects = [] })
                .WithUnit(enemy with { ExperienceGained = 1 })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal),
                TurnQueue = [],
                CurrentRound = BattleRound.Normal(1),
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void ParalyzedUnitTest()
    {
        UnitDefinition retreatingDefinition = TestHelpers.SimpleUnitDefinition("retreating", maxHitpoints: 100, initiative: 30, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 50, damage: 20);

        BattleUnit retreating = new(new BattleUnitId(0), null, retreatingDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [new BattleRetreatStatusEffect(), new BattleParalyzeStatusEffect(2)], null, [], null, false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(retreatingDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(retreating, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleSkipCommand(enemy.Id), [
            new BattleUnitTurnSkippedEvent(enemy.Id),
            new BattleUnitTurnEndingEvent(enemy.Id),
            new BattleUnitTurnEndedEvent(enemy.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(retreating.Id),
            new BattleUnitTurnSkippedEvent(retreating.Id),
            new BattleUnitTurnEndingEvent(retreating.Id),
            new BattleUnitTurnEndedEvent(retreating.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(
                BattleRound.Normal(2),
                [new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(retreating with { StatusEffects = [new BattleRetreatStatusEffect(), new BattleParalyzeStatusEffect(1)] })
                .WithUnit(enemy)
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(2)
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void EnemyAttackTest()
    {
        UnitDefinition retreatingDefinition = TestHelpers.SimpleUnitDefinition("retreating", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 30, damage: 20);

        BattleUnit retreating = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, retreatingDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(retreatingDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(retreating, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleRetreatCommand(retreating.Id));

        engine.CheckCommand(new BattleAttackCommand(enemy.Id, new BattleAttackUnitTarget(retreating.Id)), [
            new BattleUnitAttackEvent(enemy.Id, new BattleAttackUnitTarget(retreating.Id), [new UnitAttackDefinitionId("enemy")]),
            new BattleUnitAttackedEvent(enemy.Id, retreating.Id, new UnitAttackDefinitionId("enemy")),
            new BattleUnitAttackHitEvent(enemy.Id, retreating.Id, new UnitAttackDefinitionId("enemy"), 20),
            new BattleUnitTurnEndingEvent(enemy.Id),
            new BattleUnitTurnEndedEvent(enemy.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(retreating.Id, BattleUnitTurnType.Normal)),
            new BattleUnitRetreatedEvent(retreating.Id),
            new BattleUnitTurnEndingEvent(retreating.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(enemy.Id, 1)
        ]);

        BattleState expected = engine.State
                .WithUnit(retreating with { Hitpoints = 80, StatusEffects = [new BattleRetreatedStatusEffect()] })
            with
            {
                CurrentRound = BattleRound.Normal(2),
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void KillBeforeEscapingTest()
    {
        UnitDefinition fragileDefinition = TestHelpers.SimpleUnitDefinition("fragile", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 30, damage: 20);

        BattleUnit fragile = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, fragileDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(fragileDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(fragile, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleRetreatCommand(fragile.Id));

        engine.CheckCommand(new BattleAttackCommand(enemy.Id, new BattleAttackUnitTarget(fragile.Id)), [
            new BattleUnitAttackEvent(enemy.Id, new BattleAttackUnitTarget(fragile.Id), [new UnitAttackDefinitionId("enemy")]),
            new BattleUnitAttackedEvent(enemy.Id, fragile.Id, new UnitAttackDefinitionId("enemy")),
            new BattleUnitAttackHitEvent(enemy.Id, fragile.Id, new UnitAttackDefinitionId("enemy"), 20),
            new BattleUnitDiedEvent(fragile.Id),
            new BattleUnitTurnEndingEvent(enemy.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(enemy.Id, 1)
        ]);

        BattleState expected = engine.State
                .WithUnit(fragile with { Hitpoints = 0, StatusEffects = [] })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal),
                TurnQueue = [],
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void FastRetreatTest()
    {
        UnitDefinition retreatingDefinition = TestHelpers.SimpleUnitDefinition("retreating", maxHitpoints: 100, initiative: 50, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 30, damage: 20);

        UnitModifierDefinition fastRetreatModifier = TestHelpers.SimpleModifier("fastretreat") with
        {
            FastRetreat = true
        };

        BattleUnit retreating = new(new BattleUnitId(0), null, retreatingDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [UnitModifier.Permanent(fastRetreatModifier.Id)], [], null, [], null, false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(retreatingDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(fastRetreatModifier)
            .WithAttacker(retreating, GridPosition.FrontBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleRetreatCommand(retreating.Id), [
            new BattleUnitRetreatedEvent(retreating.Id),
            new BattleUnitTurnEndingEvent(retreating.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(enemy.Id, 1)
        ]);

        BattleState expected = engine.State
                .WithUnit(retreating with { StatusEffects = [new BattleRetreatedStatusEffect()] })
            with
            {
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void NotUnitTurnTest()
    {
        UnitDefinition fastDefinition = TestHelpers.SimpleUnitDefinition("fast", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition slowDefinition = TestHelpers.SimpleUnitDefinition("slow", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit fast = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, fastDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit slow = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, slowDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(fastDefinition)
            .WithDefinition(slowDefinition)
            .WithAttacker(fast, GridPosition.FrontBottom)
            .WithDefender(slow, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(new BattleRetreatCommand(slow.Id), BattleCommandError.NotUnitTurn);
    }

    [Test]
    public void DefenderTest()
    {
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);
        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(defenderDefinition)
            .WithDefinition(attackerDefinition)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleRetreatCommand(defender.Id), [
            new BattleUnitRetreatingEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(defender with { StatusEffects = [new BattleRetreatStatusEffect()] })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                TurnQueue = []
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void FortifiedDefenderTest()
    {
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 50, damage: 5);
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);
        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(defenderDefinition)
            .WithDefinition(attackerDefinition)
            .WithModifiers(new BattleModifiers(
                Attacker: BattleSideModifiers.None,
                Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false)))
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(new BattleRetreatCommand(defender.Id), BattleCommandError.UnitRetreatNotAllowed);
    }

    [Test]
    public void AllReachAttackRetreatedUnitTest()
    {
        UnitDefinition goneDefinition = TestHelpers.SimpleUnitDefinition("gone", maxHitpoints: 100, initiative: 60, damage: 5);
        UnitDefinition stayingDefinition = TestHelpers.SimpleUnitDefinition("staying", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitDefinition mageDefinition = TestHelpers.SimpleUnitDefinition("mage", maxHitpoints: 100, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("mage"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.All)
        };

        BattleUnit gone = new(new BattleUnitId(0), null, goneDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [new BattleRetreatedStatusEffect()], null, [], null, false);
        BattleUnit staying = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, stayingDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit mage = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, mageDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(goneDefinition)
            .WithDefinition(stayingDefinition)
            .WithDefinition(mageDefinition)
            .WithAttacker(gone, GridPosition.FrontBottom)
            .WithAttacker(staying, GridPosition.FrontCenter)
            .WithDefender(mage, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(mage.Id, new BattleAttackUnitTarget(staying.Id)), [
            new BattleUnitAttackEvent(mage.Id, new BattleAttackUnitTarget(staying.Id), [new UnitAttackDefinitionId("mage")]),
            new BattleUnitAttackedEvent(mage.Id, staying.Id, new UnitAttackDefinitionId("mage")),
            new BattleUnitAttackHitEvent(mage.Id, staying.Id, new UnitAttackDefinitionId("mage"), 5),
            new BattleUnitTurnEndingEvent(mage.Id),
            new BattleUnitTurnEndedEvent(mage.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(staying.Id, BattleUnitTurnType.Normal))
        ]);

        BattleState expected = engine.State
                .WithUnit(staying with { Hitpoints = 95 })
            with
            {
                CurrentUnitTurn = new BattleUnitTurn(staying.Id, BattleUnitTurnType.Normal),
                TurnQueue = []
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }
}
