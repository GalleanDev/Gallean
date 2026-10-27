namespace Gallean.Engine.Tests;

public sealed class BattleEngineDoppelgangerAttackTests
{
    private static UnitDefinition DoppelgangerDefinition()
    {
        return TestHelpers.SimpleUnitDefinition("doppelganger", maxHitpoints: 60, initiative: 50, damage: 5) with
        {
            Level = 3,
            Attack = TestHelpers.SimpleAttack(
                    new UnitAttackDefinitionId("doppelganger"),
                    initiative: 50,
                    damage: 5,
                    accuracy: 100,
                    reach: UnitAttackReach.Any) with
                {
                    Type = UnitAttackType.Doppelganger,
                    IsInfinite = true
                }
        };
    }

    [Test]
    public void TransformsIntoTargetTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition();
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 120, initiative: 40, damage: 25) with
        {
            Level = 2
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 30, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit knight = new(new BattleUnitId(1), null, knightDefinition.Id, BattleSide.Defender, 120, 0, 0, 2, [], [], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(knightDefinition)
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(knight, GridPosition.FrontBottom)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(doppelganger, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(knight, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Doppelganger),
                TurnQueue = [],
                CurrentRound = BattleRound.Opening(1)
            }));

        engine.CheckCommand(new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(knight.Id)),
        [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(knight.Id), [doppelgangerDefinition.Attack.Id]),
            new BattleUnitTransformAppliedEvent(doppelganger.Id, doppelganger.Id, BattleTransformType.Doppelganger, knightDefinition.Id, 2, 60, 120, null),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleUnitTurnEndedEvent(doppelganger.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(1), [new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(knight.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit transformed = doppelganger with
        {
            DefinitionId = knightDefinition.Id,
            Hitpoints = 60,
            Level = 2,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.Doppelganger, doppelgangerDefinition.Id, 3, [], null)]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(transformed, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(knight, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal)
            }));

        engine.CheckCommand(new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(knight.Id)),
        [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(knight.Id), [knightDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(doppelganger.Id, knight.Id, knightDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(doppelganger.Id, knight.Id, knightDefinition.Attack.Id, 25),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleUnitTurnEndedEvent(doppelganger.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(knight.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void TransformsIntoAllyTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition();
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 40, initiative: 30, damage: 0) with
        {
            Level = 2
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 20, damage: 5);

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit healer = new(new BattleUnitId(1), null, healerDefinition.Id, BattleSide.Attacker, 40, 0, 0, 2, [], [], null, [], null, false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(healerDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithAttacker(healer, GridPosition.BackBottom)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(healer.Id)),
        [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(healer.Id), [doppelgangerDefinition.Attack.Id]),
            new BattleUnitTransformAppliedEvent(doppelganger.Id, doppelganger.Id, BattleTransformType.Doppelganger, healerDefinition.Id, 2, 40, 40, null),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleUnitTurnEndedEvent(doppelganger.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(1), [new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(healer.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(enemy.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal))
        ]);

        BattleUnit transformed = doppelganger with
        {
            DefinitionId = healerDefinition.Id,
            Hitpoints = 40,
            Level = 2,
            StatusEffects = [new BattleTransformStatusEffect(BattleTransformType.Doppelganger, doppelgangerDefinition.Id, 3, [], null)]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(transformed, GridPosition.FrontBottom, false)
                    .Place(healer, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(enemy, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal)
            }));
    }

    [Test]
    public void InvalidTargetsTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition();
        UnitDefinition guardianDefinition = TestHelpers.SimpleUnitDefinition("guardian", maxHitpoints: 200, initiative: 20, damage: 30) with
        {
            Category = UnitCategory.Guardian
        };
        UnitDefinition giantDefinition = TestHelpers.SimpleUnitDefinition("giant", maxHitpoints: 300, initiative: 30, damage: 40) with
        {
            IsLarge = true
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit twin = new(new BattleUnitId(1), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit guardian = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, guardianDefinition.Id, BattleSide.Defender, hitpoints: 200);
        BattleUnit giant = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, giantDefinition.Id, BattleSide.Defender, hitpoints: 300);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(guardianDefinition)
            .WithDefinition(giantDefinition)
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithAttacker(twin, GridPosition.BackBottom)
            .WithDefender(guardian, GridPosition.FrontBottom)
            .WithDefender(giant, GridPosition.FrontTop)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(guardian.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(giant.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(twin.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(doppelganger.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
    }

    [Test]
    public void OpeningTurnTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition();
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 120, initiative: 90, damage: 25) with
        {
            Level = 2
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit knight = new(new BattleUnitId(1), null, knightDefinition.Id, BattleSide.Defender, 120, 0, 0, 2, [], [], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(knightDefinition)
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(knight, GridPosition.FrontBottom)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(doppelganger, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(knight, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Doppelganger),
                TurnQueue = [],
                CurrentRound = BattleRound.Opening(1)
            }));

        engine.CheckCommandError(new BattleWaitCommand(doppelganger.Id), BattleCommandError.ActionNotAvailable);
        engine.CheckCommandError(new BattleRetreatCommand(doppelganger.Id), BattleCommandError.ActionNotAvailable);

        _ = engine.DispatchCommand(new BattleSkipCommand(doppelganger.Id));

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(doppelganger, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(knight, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(knight.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void TransformedExperienceTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition() with
        {
            ExperienceKilled = 40
        };
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 10, initiative: 40, damage: 25) with
        {
            Level = 2,
            ExperienceKilled = 400
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit knight = new(new BattleUnitId(1), null, knightDefinition.Id, BattleSide.Defender, 10, 0, 0, 2, [], [], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(knightDefinition)
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(knight, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(knight.Id)));

        engine.CheckCommand(new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(knight.Id)),
        [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(knight.Id), [knightDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(doppelganger.Id, knight.Id, knightDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(doppelganger.Id, knight.Id, knightDefinition.Attack.Id, 25),
            new BattleUnitDiedEvent(knight.Id),
            new BattleUnitExperienceGainedEvent(doppelganger.Id, 400),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleRoundEndedEvent(),
            new BattleUnitTransformRestoredEvent(doppelganger.Id, BattleTransformType.Doppelganger, doppelgangerDefinition.Id, 60, 60),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(doppelganger.Id, 400)
        ]);

        BattleUnit restored = doppelganger with
        {
            Hitpoints = 60,
            ExperienceGained = 400
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(restored, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(knight with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                    .Build(),
                Outcome = new BattleOutcome(BattleSide.Attacker)
            }));
    }

    [Test]
    public void KilledTransformedExperienceTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition() with
        {
            ExperienceKilled = 40
        };
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 20, initiative: 40, damage: 25) with
        {
            Level = 2,
            ExperienceKilled = 400
        };
        UnitDefinition archerDefinition = TestHelpers.SimpleUnitDefinition("archer", maxHitpoints: 100, initiative: 90, damage: 100);

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit knight = new(new BattleUnitId(1), null, knightDefinition.Id, BattleSide.Defender, 20, 0, 0, 2, [], [], null, [], null, false);
        BattleUnit archer = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, archerDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(knightDefinition)
            .WithDefinition(archerDefinition)
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(knight, GridPosition.FrontBottom)
            .WithDefender(archer, GridPosition.BackBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(knight.Id)));
        engine.CheckCommand(new BattleAttackCommand(archer.Id, new BattleAttackUnitTarget(doppelganger.Id)),
        [
            new BattleUnitAttackEvent(archer.Id, new BattleAttackUnitTarget(doppelganger.Id), [archerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(archer.Id, doppelganger.Id, archerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(archer.Id, doppelganger.Id, archerDefinition.Attack.Id, 100),
            new BattleUnitDiedEvent(doppelganger.Id),
            new BattleUnitExperienceGainedEvent(knight.Id, 20),
            new BattleUnitExperienceGainedEvent(archer.Id, 20),
            new BattleUnitTurnEndingEvent(archer.Id),
            new BattleRoundEndedEvent(),
            new BattleUnitTransformRestoredEvent(doppelganger.Id, BattleTransformType.Doppelganger, doppelgangerDefinition.Id, 0, 60 ),
            new BattleEndedEvent(BattleSide.Defender),
            new BattleUnitExperienceGainedEvent(knight.Id, 20),
            new BattleUnitExperienceGainedEvent(archer.Id, 20)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(doppelganger with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(knight with { ExperienceGained = 20 }, GridPosition.FrontBottom, false)
                    .Place(archer with { ExperienceGained = 20 }, GridPosition.BackBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(archer.Id, BattleUnitTurnType.Normal),
                TurnQueue = [new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(knight.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(1),
                Outcome = new BattleOutcome(BattleSide.Defender)
            }));
    }

    [Test]
    public void IllusionTargetTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition();
        UnitDefinition illusionDefinition = TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 40, initiative: 20, damage: 10) with
        {
            Category = UnitCategory.Illusion
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit illusion = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, illusionDefinition.Id, BattleSide.Defender, hitpoints: 40);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(illusionDefinition)
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(illusion, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(illusion.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
    }

    [Test]
    public void AttackTargetTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition();
        UnitDefinition otherDoppelgangerDefinition = TestHelpers.SimpleUnitDefinition("other-doppelganger", maxHitpoints: 80, initiative: 40, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(
                    new UnitAttackDefinitionId("other-doppelganger-attack"),
                    initiative: 40,
                    damage: 5,
                    accuracy: 100,
                    reach: UnitAttackReach.Any) with
                {
                    Type = UnitAttackType.Doppelganger,
                    IsInfinite = true
                }
        };
        UnitDefinition secondaryDoppelgangerDefinition = TestHelpers.SimpleUnitDefinition("secondary-doppelganger", maxHitpoints: 90, initiative: 30, damage: 5) with
        {
            SecondaryAttack = TestHelpers.SimpleAttack(
                    new UnitAttackDefinitionId("secondary-doppelganger-attack"),
                    initiative: 30,
                    damage: 5,
                    accuracy: 100,
                    reach: UnitAttackReach.Any) with
                {
                    Type = UnitAttackType.Doppelganger,
                    IsInfinite = true
                }
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit otherDoppelganger = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, otherDoppelgangerDefinition.Id, BattleSide.Defender, hitpoints: 80);
        BattleUnit secondaryDoppelganger = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, secondaryDoppelgangerDefinition.Id, BattleSide.Defender, hitpoints: 90);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(otherDoppelgangerDefinition)
            .WithDefinition(secondaryDoppelgangerDefinition)
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(otherDoppelganger, GridPosition.FrontBottom)
            .WithDefender(secondaryDoppelganger, GridPosition.FrontTop)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(otherDoppelganger.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);

        engine.CheckCommand(new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(secondaryDoppelganger.Id)),
        [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(secondaryDoppelganger.Id), [doppelgangerDefinition.Attack.Id]),
            new BattleUnitTransformAppliedEvent(doppelganger.Id, doppelganger.Id, BattleTransformType.Doppelganger, secondaryDoppelgangerDefinition.Id, 1, 90, 90, null),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleUnitTurnEndedEvent(doppelganger.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(otherDoppelganger.Id, BattleUnitTurnType.Doppelganger))
        ]);
    }

    [Test]
    public void ImmuneTargetTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition();
        UnitDefinition classWardDefinition = TestHelpers.SimpleUnitDefinition("class-warded", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            Wards = [new UnitAttackClassWard(UnitAttackType.Doppelganger, UnitWardCategory.Always)]
        };
        UnitDefinition sourceWardDefinition = TestHelpers.SimpleUnitDefinition("source-warded", maxHitpoints: 110, initiative: 30, damage: 10) with
        {
            Wards = [new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Once)]
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit classWarded = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, classWardDefinition.Id, BattleSide.Defender, hitpoints: 100);
        BattleUnit sourceWarded = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, sourceWardDefinition.Id, BattleSide.Defender, hitpoints: 110);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(classWardDefinition)
            .WithDefinition(sourceWardDefinition)
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(classWarded, GridPosition.FrontBottom)
            .WithDefender(sourceWarded, GridPosition.FrontTop)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(classWarded.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(sourceWarded.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
    }

    [Test]
    public void UnrelatedWardTargetTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition();
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 120, initiative: 40, damage: 25) with
        {
            Level = 2,
            Wards = [new UnitAttackClassWard(UnitAttackType.Poison, UnitWardCategory.Always), new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Always)]
        };

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 30, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit knight = new(new BattleUnitId(1), null, knightDefinition.Id, BattleSide.Defender, 120, 0, 0, 2, [], [], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(knightDefinition)
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(knight, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(knight.Id)),
        [
            new BattleUnitAttackEvent(doppelganger.Id, new BattleAttackUnitTarget(knight.Id), [doppelgangerDefinition.Attack.Id]),
            new BattleUnitTransformAppliedEvent(doppelganger.Id, doppelganger.Id, BattleTransformType.Doppelganger, knightDefinition.Id, 2, 60, 120, null),
            new BattleUnitTurnEndingEvent(doppelganger.Id),
            new BattleUnitTurnEndedEvent(doppelganger.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(1), [new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(knight.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal))
        ]);
    }

    [Test]
    public void ExcludedTargetsTest()
    {
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition();
        UnitDefinition drullianDefinition = TestHelpers.SimpleUnitDefinition("G000UU8003", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitDefinition lachlanDefinition = TestHelpers.SimpleUnitDefinition("G000UU8006", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitDefinition drullianUpgradedDefinition = TestHelpers.SimpleUnitDefinition("G000UU8103", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitDefinition lachlanUpgradedDefinition = TestHelpers.SimpleUnitDefinition("G000UU8106", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitDefinition allemonDefinition = TestHelpers.SimpleUnitDefinition("G000UU8150", maxHitpoints: 100, initiative: 40, damage: 10);

        BattleUnit doppelganger = new(new BattleUnitId(0), null, doppelgangerDefinition.Id, BattleSide.Attacker, 60, 0, 0, 3, [], [], null, [], null, false);
        BattleUnit drullian = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, drullianDefinition.Id, BattleSide.Defender, hitpoints: 100);
        BattleUnit lachlan = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, lachlanDefinition.Id, BattleSide.Defender, hitpoints: 100);
        BattleUnit drullianUpgraded = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, drullianUpgradedDefinition.Id, BattleSide.Defender, hitpoints: 100);
        BattleUnit lachlanUpgraded = TestHelpers.SimpleBattleUnit(new BattleUnitId(4), null, lachlanUpgradedDefinition.Id, BattleSide.Defender, hitpoints: 100);
        BattleUnit allemon = TestHelpers.SimpleBattleUnit(new BattleUnitId(5), null, allemonDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(drullianDefinition)
            .WithDefinition(lachlanDefinition)
            .WithDefinition(drullianUpgradedDefinition)
            .WithDefinition(lachlanUpgradedDefinition)
            .WithDefinition(allemonDefinition)
            .WithAttacker(doppelganger, GridPosition.FrontBottom)
            .WithDefender(drullian, GridPosition.FrontBottom)
            .WithDefender(lachlan, GridPosition.FrontCenter)
            .WithDefender(drullianUpgraded, GridPosition.FrontTop)
            .WithDefender(lachlanUpgraded, GridPosition.BackBottom)
            .WithDefender(allemon, GridPosition.BackCenter)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(drullian.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(lachlan.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(drullianUpgraded.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(lachlanUpgraded.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
        engine.CheckCommandError(
            new BattleAttackCommand(doppelganger.Id, new BattleAttackUnitTarget(allemon.Id)),
            BattleCommandError.DoppelgangerTargetInvalid);
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
        UnitDefinition doppelgangerDefinition = DoppelgangerDefinition();
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 120, initiative: 40, damage: 25) with
        {
            Level = 2
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 20, damage: 5);

        BattleTransformStatusEffect transformEffect = new(BattleTransformType.Doppelganger, doppelgangerDefinition.Id, 3, [], null);
        BattleUnit cure = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, cureDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit doppelganger = new(
            new BattleUnitId(1),
            null,
            knightDefinition.Id,
            BattleSide.Attacker,
            60,
            0,
            0,
            2,
            [],
            [transformEffect, new BattlePoisonStatusEffect(3, 10)],
            null,
            [],
            Name: null,
            IsTypeLocked: false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(cureDefinition)
            .WithDefinition(doppelgangerDefinition)
            .WithDefinition(knightDefinition)
            .WithDefinition(enemyDefinition)
            .WithAttacker(cure, GridPosition.FrontBottom)
            .WithAttacker(doppelganger, GridPosition.FrontCenter)
            .WithDefender(enemy, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(cure.Id, new BattleAttackUnitTarget(doppelganger.Id)),
        [
            new BattleUnitAttackEvent(cure.Id, new BattleAttackUnitTarget(doppelganger.Id), [cureDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(cure.Id, doppelganger.Id, cureDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(cure.Id, doppelganger.Id, cureDefinition.Attack.Id, 0),
            new BattleUnitCuredEvent(doppelganger.Id),
            new BattleUnitTurnEndingEvent(cure.Id),
            new BattleUnitTurnEndedEvent(cure.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(cure, GridPosition.FrontBottom, false)
                    .Place(doppelganger with { StatusEffects = [transformEffect, new BattleCureStatusEffect()] }, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(enemy, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(doppelganger.Id, BattleUnitTurnType.Normal)
            }));
    }
}
