namespace Gallean.Engine.Tests;

public sealed class BattleEngineReduceLevelAttackTests
{
    private static UnitDefinition AttackerDefinition(bool isInfinite)
    {
        return TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 50, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(
                    new UnitAttackDefinitionId("attacker"),
                    initiative: 60,
                    damage: 0,
                    accuracy: 100,
                    reach: UnitAttackReach.Any) with
                {
                    Type = UnitAttackType.ReduceLevel,
                    IsInfinite = isInfinite
                }
        };
    }

    private static UnitDynamicUpgradeDefinition VeteranUpgrade()
    {
        return new UnitDynamicUpgradeDefinition(
            new UnitDefinitionId("veteran"),
            UpgradeChangeLevel: 10,
            UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 5 },
            UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, Damage = 2 });
    }

    [Test]
    public void ReducesLevelTest()
    {
        UnitDefinition attackerDefinition = AttackerDefinition(isInfinite: true);
        UnitDefinition veteranDefinition = TestHelpers.SimpleUnitDefinition("veteran", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Level = 2
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit veteran = new(new BattleUnitId(1), null, veteranDefinition.Id, BattleSide.Defender, 120, 0, 0, 4, [], [], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(veteranDefinition)
            .WithDefinition(VeteranUpgrade())
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(veteran, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(veteran.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(veteran.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, veteran.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, veteran.Id, attackerDefinition.Attack.Id, 0),
            new BattleUnitTransformAppliedEvent(attacker.Id, veteran.Id, BattleTransformType.LevelDrain, veteranDefinition.Id, 3, 110, 110, null),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(veteran.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit reduced = veteran with
        {
            Hitpoints = 110,
            Level = 3,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.LevelDrain, veteranDefinition.Id, 4, [], null)]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(reduced, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(veteran.Id, BattleUnitTurnType.Normal)
            }));

        engine.CheckCommand(new BattleAttackCommand(veteran.Id, new BattleAttackUnitTarget(attacker.Id)),
        [
            new BattleUnitAttackEvent(veteran.Id, new BattleAttackUnitTarget(attacker.Id), [veteranDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(veteran.Id, attacker.Id, veteranDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(veteran.Id, attacker.Id, veteranDefinition.Attack.Id, 15),
            new BattleUnitTurnEndingEvent(veteran.Id),
            new BattleUnitTurnEndedEvent(veteran.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(veteran.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { Hitpoints = 35 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(reduced, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void ReducesToPreviousUnitTest()
    {
        UnitWard veteranWard = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Always);
        UnitWard recruitWard = new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Always);
        UnitDefinition attackerDefinition = AttackerDefinition(isInfinite: true);
        UnitDefinition recruitDefinition = TestHelpers.SimpleUnitDefinition("recruit", maxHitpoints: 60, initiative: 30, damage: 4) with
        {
            Wards = [recruitWard]
        };
        UnitDefinition veteranDefinition = TestHelpers.SimpleUnitDefinition("veteran", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Level = 2,
            PreviousUnitId = recruitDefinition.Id,
            Wards = [veteranWard]
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit veteran = new(new BattleUnitId(1), null, veteranDefinition.Id, BattleSide.Defender, 50, 0, 0, 2, [], [], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(recruitDefinition)
            .WithDefinition(veteranDefinition)
            .WithDefinition(VeteranUpgrade())
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(veteran, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(veteran.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(veteran.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, veteran.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, veteran.Id, attackerDefinition.Attack.Id, 0),
            new BattleUnitTransformAppliedEvent(attacker.Id, veteran.Id, BattleTransformType.LevelDrain, recruitDefinition.Id, 1, 30, 60, null),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(veteran.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit reduced = veteran with
        {
            DefinitionId = recruitDefinition.Id,
            Hitpoints = 30,
            Level = 1,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.LevelDrain, veteranDefinition.Id, 2, [veteranWard], null)],
            Wards = [recruitWard]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(reduced, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(veteran.Id, BattleUnitTurnType.Normal)
            }));
    }

    [Test]
    public void MinimumLevelTargetTest()
    {
        UnitDefinition attackerDefinition = AttackerDefinition(isInfinite: true);
        UnitDefinition recruitDefinition = TestHelpers.SimpleUnitDefinition("recruit", maxHitpoints: 100, initiative: 50, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit recruit = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, recruitDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(recruitDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(recruit, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(recruit.Id)),
            BattleCommandError.TargetAtMinimumLevel);
    }

    [Test]
    public void NoPreviousUnitTargetTest()
    {
        UnitDefinition attackerDefinition = AttackerDefinition(isInfinite: true);
        UnitDefinition veteranDefinition = TestHelpers.SimpleUnitDefinition("veteran", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Level = 2
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit veteran = new(new BattleUnitId(1), null, veteranDefinition.Id, BattleSide.Defender, 100, 0, 0, 2, [], [], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(veteranDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(veteran, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(veteran.Id)),
            BattleCommandError.TargetAtMinimumLevel);
    }

    [Test]
    public void DurationTest()
    {
        UnitDefinition attackerDefinition = AttackerDefinition(isInfinite: false);
        UnitDefinition veteranDefinition = TestHelpers.SimpleUnitDefinition("veteran", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Level = 2
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit veteran = new(new BattleUnitId(1), null, veteranDefinition.Id, BattleSide.Defender, 120, 0, 0, 4, [], [], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(veteranDefinition)
            .WithDefinition(VeteranUpgrade())
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(veteran, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(veteran.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(veteran.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, veteran.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, veteran.Id, attackerDefinition.Attack.Id, 0),
            new BattleUnitTransformAppliedEvent(attacker.Id, veteran.Id, BattleTransformType.LevelDrain, veteranDefinition.Id, 3, 110, 110, 1),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(veteran.Id, BattleUnitTurnType.Normal)),
            new BattleUnitTransformRestoredEvent(veteran.Id, BattleTransformType.LevelDrain, veteranDefinition.Id, 120, 120 ),
            new BattleUnitEffectDurationDecreasedEvent(veteran.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(veteran with { Hitpoints = 120 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(veteran.Id, BattleUnitTurnType.Normal)
            }));

        engine.CheckCommand(new BattleAttackCommand(veteran.Id, new BattleAttackUnitTarget(attacker.Id)),
        [
            new BattleUnitAttackEvent(veteran.Id, new BattleAttackUnitTarget(attacker.Id), [veteranDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(veteran.Id, attacker.Id, veteranDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(veteran.Id, attacker.Id, veteranDefinition.Attack.Id, 20),
            new BattleUnitTurnEndingEvent(veteran.Id),
            new BattleUnitTurnEndedEvent(veteran.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(veteran.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { Hitpoints = 30 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(veteran with { Hitpoints = 120 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void CureTest()
    {
        UnitDefinition cureDefinition = TestHelpers.SimpleUnitDefinition("cure", maxHitpoints: 10, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(
                    new UnitAttackDefinitionId("cure"),
                    initiative: 60,
                    damage: 0,
                    accuracy: 100,
                    reach: UnitAttackReach.Any) with
                {
                    Type = UnitAttackType.Cure
                }
        };
        UnitDefinition veteranDefinition = TestHelpers.SimpleUnitDefinition("veteran", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Level = 2
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 30, damage: 2);

        BattleTransformStatusEffect drainEffect = new(BattleTransformType.LevelDrain, veteranDefinition.Id, 4, [], null);
        BattleUnit cure = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, cureDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit reduced = new(
            new BattleUnitId(1),
            null,
            veteranDefinition.Id,
            BattleSide.Attacker,
            110,
            0,
            0,
            3,
            [],
            [drainEffect, new BattlePoisonStatusEffect(3, 10)],
            null,
            [],
            Name: null,
            IsTypeLocked: false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(cureDefinition)
            .WithDefinition(veteranDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(VeteranUpgrade())
            .WithAttacker(cure, GridPosition.FrontBottom)
            .WithAttacker(reduced, GridPosition.FrontCenter)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(cure.Id, new BattleAttackUnitTarget(reduced.Id)),
        [
            new BattleUnitAttackEvent(cure.Id, new BattleAttackUnitTarget(reduced.Id), [cureDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(cure.Id, reduced.Id, cureDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(cure.Id, reduced.Id, cureDefinition.Attack.Id, 0),
            new BattleUnitCuredEvent(reduced.Id),
            new BattleUnitTurnEndingEvent(cure.Id),
            new BattleUnitTurnEndedEvent(cure.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(reduced.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit cured = reduced with
        {
            StatusEffects = [drainEffect, new BattleCureStatusEffect()]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(cure, GridPosition.FrontBottom, false)
                    .Place(cured, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(enemy, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(reduced.Id, BattleUnitTurnType.Normal)
            }));

        engine.CheckCommand(new BattleAttackCommand(reduced.Id, new BattleAttackUnitTarget(enemy.Id)),
        [
            new BattleUnitAttackEvent(reduced.Id, new BattleAttackUnitTarget(enemy.Id), [veteranDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(reduced.Id, enemy.Id, veteranDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(reduced.Id, enemy.Id, veteranDefinition.Attack.Id, 15),
            new BattleUnitTurnEndingEvent(reduced.Id),
            new BattleUnitTurnEndedEvent(reduced.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void BattleEndRestoresTest()
    {
        UnitDefinition attackerDefinition = AttackerDefinition(isInfinite: true);
        UnitDefinition veteranDefinition = TestHelpers.SimpleUnitDefinition("veteran", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Level = 2
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 5);
        BattleUnit veteran = new(new BattleUnitId(1), null, veteranDefinition.Id, BattleSide.Defender, 120, 0, 0, 4, [], [], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(veteranDefinition)
            .WithDefinition(VeteranUpgrade())
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(veteran, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(veteran.Id)));

        engine.CheckCommand(new BattleAttackCommand(veteran.Id, new BattleAttackUnitTarget(attacker.Id)),
        [
            new BattleUnitAttackEvent(veteran.Id, new BattleAttackUnitTarget(attacker.Id), [veteranDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(veteran.Id, attacker.Id, veteranDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(veteran.Id, attacker.Id, veteranDefinition.Attack.Id, 15),
            new BattleUnitDiedEvent(attacker.Id),
            new BattleUnitTurnEndingEvent(veteran.Id),
            new BattleRoundEndedEvent(),
            new BattleUnitTransformRestoredEvent(veteran.Id, BattleTransformType.LevelDrain, veteranDefinition.Id, 120, 120 ),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(veteran.Id, 1)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(veteran with { Hitpoints = 120, ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                    .Build(),
                Outcome = new BattleOutcome(BattleSide.Defender)
            }));
    }
}
