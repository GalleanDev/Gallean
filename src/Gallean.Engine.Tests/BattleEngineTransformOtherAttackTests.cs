namespace Gallean.Engine.Tests;

public sealed class BattleEngineTransformOtherAttackTests
{
    private static UnitDefinition TransformAttackerDefinition(
        bool isInfinite,
        UnitDefinitionId? smallTransformUnitId,
        UnitDefinitionId? largeTransformUnitId)
    {
        return TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("transform"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.TransformEnemy,
                Source = UnitAttackSource.Mind,
                IsInfinite = isInfinite,
                SmallTransformUnitId = smallTransformUnitId,
                LargeTransformUnitId = largeTransformUnitId
            }
        };
    }

    [Test]
    public void DurationTest()
    {
        UnitDefinition transformedDefinition = TestHelpers.SimpleUnitDefinition("transformed", maxHitpoints: 50, initiative: 40, damage: 2);
        UnitDefinition attackerDefinition = TransformAttackerDefinition(true, transformedDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 80);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(transformedDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 3, 1, 1, 3))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id, 0),
            new BattleUnitTransformAppliedEvent(attacker.Id, defender.Id, BattleTransformType.Transform, transformedDefinition.Id, 1, 40, 3),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id)
        ]);

        _ = engine.DispatchCommand(new BattleSkipCommand(defender.Id));

        engine.CheckCommand(new BattleSkipCommand(attacker.Id),
        [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id)
        ]);

        _ = engine.DispatchCommand(new BattleSkipCommand(defender.Id));

        engine.CheckCommand(new BattleSkipCommand(attacker.Id),
        [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitTransformRestoredEvent(defender.Id, BattleTransformType.Transform, defenderDefinition.Id),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(3)
            }));
    }

    [Test]
    public void ExpirationTest()
    {
        UnitDefinition transformedDefinition = TestHelpers.SimpleUnitDefinition("transformed", maxHitpoints: 50, initiative: 40, damage: 2);
        UnitDefinition attackerDefinition = TransformAttackerDefinition(true, transformedDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 80);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(transformedDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 2, 1, 1, 2))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id, 0),
            new BattleUnitTransformAppliedEvent(attacker.Id, defender.Id, BattleTransformType.Transform, transformedDefinition.Id, 1, 40, 2),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id)
        ]);

        _ = engine.DispatchCommand(new BattleSkipCommand(defender.Id));

        engine.CheckCommand(new BattleSkipCommand(attacker.Id),
        [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)),
            new BattleUnitTransformRestoredEvent(defender.Id, BattleTransformType.Transform, defenderDefinition.Id),
            new BattleUnitEffectDurationDecreasedEvent(defender.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void LargeTargetTest()
    {
        UnitWard originalWard = new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Once);
        UnitWard transformedWard = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Always);
        UnitDefinition smallTransformDefinition = TestHelpers.SimpleUnitDefinition("small-transform", maxHitpoints: 30, initiative: 30, damage: 2);
        UnitDefinition largeTransformDefinition = TestHelpers.SimpleUnitDefinition("large-transform", maxHitpoints: 60, initiative: 30, damage: 3) with
        {
            IsLarge = true,
            Wards = [transformedWard]
        };
        UnitDefinition attackerDefinition = TransformAttackerDefinition(
            false,
            smallTransformDefinition.Id,
            largeTransformDefinition.Id);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            IsLarge = true,
            Wards = [originalWard]
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(smallTransformDefinition)
            .WithDefinition(largeTransformDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id, 0),
            new BattleUnitTransformAppliedEvent(attacker.Id, defender.Id, BattleTransformType.Transform, largeTransformDefinition.Id, 1, 60, null),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit transformed = defender with
        {
            DefinitionId = largeTransformDefinition.Id,
            Hitpoints = 60,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.Transform, defenderDefinition.Id, 1, [originalWard], null)],
            Wards = [transformedWard]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(transformed, GridPosition.FrontBottom, true)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)
            }));
    }

    [Test]
    public void CureTest()
    {
        UnitWard originalWard = new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Once);
        UnitWard transformedWard = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Always);
        UnitDefinition cureDefinition = TestHelpers.SimpleUnitDefinition("cure", maxHitpoints: 10, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(
                    new UnitAttackDefinitionId("cure"),
                    initiative: 50,
                    damage: 0,
                    accuracy: 100,
                    reach: UnitAttackReach.Any) with
                {
                    Type = UnitAttackType.Cure
                }
        };
        UnitDefinition originalDefinition = TestHelpers.SimpleUnitDefinition("original", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Wards = [originalWard]
        };
        UnitDefinition transformedDefinition = TestHelpers.SimpleUnitDefinition("transformed", maxHitpoints: 50, initiative: 40, damage: 2) with
        {
            Wards = [transformedWard]
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 10, initiative: 30, damage: 2);

        BattleUnit cure = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, cureDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit transformed = new(
            new BattleUnitId(1),
            null,
            transformedDefinition.Id,
            BattleSide.Attacker,
            40,
            0,
            0,
            1,
            [],
            [new BattleTransformStatusEffect(BattleTransformType.Transform, originalDefinition.Id, 1, [originalWard], null)],
            null,
            [],
            Name: null,
            IsTypeLocked: false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(cureDefinition)
            .WithDefinition(originalDefinition)
            .WithDefinition(transformedDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(cure, GridPosition.FrontBottom)
            .WithAttacker(transformed, GridPosition.FrontCenter)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(cure.Id, new BattleAttackUnitTarget(transformed.Id)),
        [
            new BattleUnitAttackEvent(cure.Id, new BattleAttackUnitTarget(transformed.Id), [cureDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(cure.Id, transformed.Id, cureDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(cure.Id, transformed.Id, cureDefinition.Attack.Id, 0),
            new BattleUnitTransformRestoredEvent(transformed.Id, BattleTransformType.Transform, originalDefinition.Id),
            new BattleUnitCuredEvent(transformed.Id),
            new BattleUnitTurnEndingEvent(cure.Id),
            new BattleUnitTurnEndedEvent(cure.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(transformed.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit restored = transformed with
        {
            DefinitionId = originalDefinition.Id,
            Hitpoints = 80,
            StatusEffects = [new BattleCureStatusEffect()],
            Wards = [originalWard]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(cure, GridPosition.FrontBottom, false)
                    .Place(restored, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(enemy, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(transformed.Id, BattleUnitTurnType.Normal)
            }));
    }

    [Test]
    public void TransformedDefinitionTest()
    {
        UnitDefinition transformedDefinition = TestHelpers.SimpleUnitDefinition("transformed", maxHitpoints: 50, initiative: 40, damage: 9);
        UnitDefinition attackerDefinition = TransformAttackerDefinition(false, transformedDefinition.Id, null);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 1) with
        {
            AttacksTwice = true
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 80);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(transformedDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)));

        engine.CheckCommand(new BattleAttackCommand(defender.Id, new BattleAttackUnitTarget(attacker.Id)),
        [
            new BattleUnitAttackEvent(defender.Id, new BattleAttackUnitTarget(attacker.Id), [transformedDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(defender.Id, attacker.Id, transformedDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(defender.Id, attacker.Id, transformedDefinition.Attack.Id, 9),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit transformed = defender with
        {
            DefinitionId = transformedDefinition.Id,
            Hitpoints = 40,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.Transform, defenderDefinition.Id, 1, [], null)]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { Hitpoints = 11 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(transformed, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void TransformOverrideTest()
    {
        UnitWard originalWard = new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Once);
        UnitWard firstTransformWard = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Always);
        UnitWard secondTransformWard = new UnitAttackSourceWard(UnitAttackSource.Earth, UnitWardCategory.Always);
        UnitDefinition originalDefinition = TestHelpers.SimpleUnitDefinition("original", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Wards = [originalWard]
        };
        UnitDefinition firstTransformDefinition = TestHelpers.SimpleUnitDefinition("first-transform", maxHitpoints: 50, initiative: 40, damage: 3) with
        {
            Wards = [firstTransformWard]
        };
        UnitDefinition secondTransformDefinition = TestHelpers.SimpleUnitDefinition("second-transform", maxHitpoints: 30, initiative: 40, damage: 2) with
        {
            Wards = [secondTransformWard]
        };
        UnitDefinition attackerDefinition = TransformAttackerDefinition(true, secondTransformDefinition.Id, null);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit target = new(
            new BattleUnitId(1),
            null,
            firstTransformDefinition.Id,
            BattleSide.Defender,
            40,
            0,
            0,
            1,
            [],
            [new BattleTransformStatusEffect(BattleTransformType.Transform, originalDefinition.Id, 1, [originalWard], null)],
            null,
            [],
            Name: null,
            IsTypeLocked: false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(originalDefinition)
            .WithDefinition(firstTransformDefinition)
            .WithDefinition(secondTransformDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(target, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 2, 1, 1, 2))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(target.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(target.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, target.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, target.Id, attackerDefinition.Attack.Id, 0),
            new BattleUnitTransformAppliedEvent(attacker.Id, target.Id, BattleTransformType.Transform, secondTransformDefinition.Id, 1, 24, 2),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(target.Id, BattleUnitTurnType.Normal)),
            new BattleUnitEffectDurationDecreasedEvent(target.Id)
        ]);

        BattleUnit overridden = target with
        {
            DefinitionId = secondTransformDefinition.Id,
            Hitpoints = 24,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.Transform, originalDefinition.Id, 1, [originalWard], 1)],
            Wards = [secondTransformWard]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(overridden, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(target.Id, BattleUnitTurnType.Normal)
            }));

        _ = engine.DispatchCommand(new BattleSkipCommand(target.Id));

        engine.CheckCommand(new BattleSkipCommand(attacker.Id),
        [
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(target.Id, BattleUnitTurnType.Normal)),
            new BattleUnitTransformRestoredEvent(target.Id, BattleTransformType.Transform, originalDefinition.Id),
            new BattleUnitEffectDurationDecreasedEvent(target.Id)
        ]);

        BattleUnit restored = target with
        {
            DefinitionId = originalDefinition.Id,
            Hitpoints = 80,
            StatusEffects = [],
            Wards = [originalWard]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(restored, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(target.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void BattleEndRestoreTransformTest()
    {
        UnitWard originalWard = new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Once);
        UnitWard transformedWard = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Always);
        UnitDefinition originalDefinition = TestHelpers.SimpleUnitDefinition("original", maxHitpoints: 100, initiative: 40, damage: 1) with
        {
            Wards = [originalWard]
        };
        UnitDefinition transformedDefinition = TestHelpers.SimpleUnitDefinition("transformed", maxHitpoints: 50, initiative: 50, damage: 10) with
        {
            Wards = [transformedWard]
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 5, initiative: 30, damage: 1);

        BattleUnit transformed = new(
            new BattleUnitId(0),
            null,
            transformedDefinition.Id,
            BattleSide.Attacker,
            40,
            0,
            0,
            1,
            [],
            [new BattleTransformStatusEffect(BattleTransformType.Transform, originalDefinition.Id, 1, [originalWard], null)],
            null,
            [],
            Name: null,
            IsTypeLocked: false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 5);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(originalDefinition)
            .WithDefinition(transformedDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(transformed, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(transformed.Id, new BattleAttackUnitTarget(defender.Id)),
        [
            new BattleUnitAttackEvent(transformed.Id, new BattleAttackUnitTarget(defender.Id), [transformedDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(transformed.Id, defender.Id, transformedDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(transformed.Id, defender.Id, transformedDefinition.Attack.Id, 10),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(transformed.Id),
            new BattleRoundEndedEvent(),
            new BattleUnitTransformRestoredEvent(transformed.Id, BattleTransformType.Transform, originalDefinition.Id),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(transformed.Id, 1)
        ]);

        BattleUnit restored = transformed with
        {
            DefinitionId = originalDefinition.Id,
            Hitpoints = 80,
            StatusEffects = [],
            Wards = [originalWard],
            ExperienceGained = 1
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(restored, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                    .Build(),
                Outcome = new BattleOutcome(BattleSide.Attacker)
            }));
    }

    [Test]
    public void BattleEndRestoreKilledTest()
    {
        UnitWard originalWard = new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Once);
        UnitDefinition originalDefinition = TestHelpers.SimpleUnitDefinition("original", maxHitpoints: 100, initiative: 30, damage: 1) with
        {
            Wards = [originalWard]
        };
        UnitDefinition transformedDefinition = TestHelpers.SimpleUnitDefinition("transformed", maxHitpoints: 50, initiative: 30, damage: 1);
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 40);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit transformed = new(
            new BattleUnitId(1),
            null,
            transformedDefinition.Id,
            BattleSide.Defender,
            40,
            0,
            0,
            1,
            [],
            [new BattleTransformStatusEffect(BattleTransformType.Transform, originalDefinition.Id, 1, [originalWard], null)],
            null,
            [],
            Name: null,
            IsTypeLocked: false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(originalDefinition)
            .WithDefinition(transformedDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(transformed, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(transformed.Id)),
        [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(transformed.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, transformed.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, transformed.Id, attackerDefinition.Attack.Id, 40),
            new BattleUnitDiedEvent(transformed.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleRoundEndedEvent(),
            new BattleUnitTransformRestoredEvent(transformed.Id, BattleTransformType.Transform, originalDefinition.Id),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(attacker.Id, 1)
        ]);

        BattleUnit restored = transformed with
        {
            DefinitionId = originalDefinition.Id,
            Hitpoints = 0,
            StatusEffects = [],
            Wards = [originalWard]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(restored, GridPosition.FrontBottom, false)
                    .Build(),
                Outcome = new BattleOutcome(BattleSide.Attacker)
            }));
    }

    [Test]
    public void BattleEndRestoreUnderSummonTest()
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
        UnitDefinition bansheeDefinition = TestHelpers.SimpleUnitDefinition("banshee", maxHitpoints: 100, initiative: 40, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("banshee"), initiative: 40, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Fear
            }
        };

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
        BattleUnit banshee = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, bansheeDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(originalDefinition)
            .WithDefinition(transformedDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(summonerDefinition)
            .WithDefinition(bansheeDefinition)
            .WithAttacker(summoner, GridPosition.FrontBottom)
            .WithAttacker(transformed, GridPosition.FrontCenter)
            .WithDefender(banshee, GridPosition.FrontBottom)
            .Build();

        BattleUnitId impId = new(3);

        _ = engine.DispatchCommand(new BattleAttackCommand(summoner.Id, new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontCenter)));

        engine.CheckCommand(new BattleAttackCommand(banshee.Id, new BattleAttackUnitTarget(summoner.Id)),
        [
            new BattleUnitAttackEvent(banshee.Id, new BattleAttackUnitTarget(summoner.Id), [new UnitAttackDefinitionId("banshee")]),
            new BattleUnitAttackedEvent(banshee.Id, summoner.Id, new UnitAttackDefinitionId("banshee")),
            new BattleUnitAttackHitEvent(banshee.Id, summoner.Id, new UnitAttackDefinitionId("banshee"), 0),
            new BattleUnitRetreatingEvent(summoner.Id),
            new BattleUnitTurnEndingEvent(banshee.Id),
            new BattleUnitTurnEndedEvent(banshee.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [
                new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal),
                new BattleUnitTurn(banshee.Id, BattleUnitTurnType.Normal),
                new BattleUnitTurn(impId, BattleUnitTurnType.Normal)
            ]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal)),
            new BattleUnitRetreatedEvent(summoner.Id),
            new BattleUnitTurnEndingEvent(summoner.Id),
            new BattleUnitUnsummonedEvent(impId),
            new BattleRoundEndedEvent(),
            new BattleUnitTransformRestoredEvent(transformed.Id, BattleTransformType.Transform, originalDefinition.Id),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(banshee.Id, 1)
        ]);

        BattleUnit restored = transformed with
        {
            DefinitionId = originalDefinition.Id,
            StatusEffects = [],
            Wards = [originalWard]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(summoner with { StatusEffects = [new BattleRetreatedStatusEffect()] }, GridPosition.FrontBottom, false)
                    .Place(restored, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(banshee with { ExperienceGained = 1 }, GridPosition.FrontBottom, false)
                    .Build(),
                Outcome = new BattleOutcome(BattleSide.Defender)
            }));
    }

    [Test]
    public void GuardianTest()
    {
        UnitDefinition transformedDefinition = TestHelpers.SimpleUnitDefinition("transformed", maxHitpoints: 50, initiative: 40, damage: 2);
        UnitDefinition attackerDefinition = TransformAttackerDefinition(false, transformedDefinition.Id, transformedDefinition.Id);
        UnitDefinition guardianDefinition = TestHelpers.SimpleUnitDefinition("guardian", maxHitpoints: 900, initiative: 40, damage: 100) with
        {
            Category = UnitCategory.Guardian
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit guardian = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, guardianDefinition.Id, BattleSide.Defender, hitpoints: 900);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(guardianDefinition)
            .WithDefinition(transformedDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(guardian, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(guardian.Id)),
            BattleCommandError.TargetIncorrect);
    }
}
