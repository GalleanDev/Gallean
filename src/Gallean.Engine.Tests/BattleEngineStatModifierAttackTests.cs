namespace Gallean.Engine.Tests;

public sealed class BattleEngineStatModifierAttackTests
{
    private static UnitDefinition IncreaseDamageDefinition(string id, int initiative, int level, bool isInfinite, UnitAttackReach reach)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 50, initiative: initiative, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id), initiative: initiative, damage: 0, accuracy: 100, reach: reach) with
            {
                Type = UnitAttackType.IncreaseDamage,
                Level = level,
                IsInfinite = isInfinite
            }
        };
    }

    private static UnitDefinition ReduceDamageDefinition(string id, int initiative, int level)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 50, initiative: initiative, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id), initiative: initiative, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.ReduceDamage,
                Level = level
            }
        };
    }

    private static UnitDefinition ReduceInitiativeDefinition(string id, int initiative)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 100, initiative: initiative, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id), initiative: initiative, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.ReduceInitiative
            }
        };
    }

    [Test]
    public void BoostSingleTurnTest()
    {
        UnitDefinition boosterDefinition = IncreaseDamageDefinition("booster", initiative: 60, level: 2, isInfinite: false, reach: UnitAttackReach.Any);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 10);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2);

        BattleUnit booster = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, boosterDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(boosterDefinition)
            .WithDefinition(warriorDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(booster, GridPosition.FrontBottom)
            .WithAttacker(warrior, GridPosition.FrontCenter)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(booster.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(booster.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("booster")]),
            new BattleUnitAttackedEvent(booster.Id, warrior.Id, new UnitAttackDefinitionId("booster")),
            new BattleUnitAttackHitEvent(booster.Id, warrior.Id, new UnitAttackDefinitionId("booster"), 0),
            new BattleUnitEffectAppliedEvent(booster.Id, warrior.Id, new BattleIncreaseDamageStatusEffect(50, false)),
            new BattleUnitTurnEndingEvent(booster.Id),
            new BattleUnitTurnEndedEvent(booster.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(booster, GridPosition.FrontBottom, false)
                    .Place(warrior with { StatusEffects = [new BattleIncreaseDamageStatusEffect(50, false)] }, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, golem.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, golem.Id, new UnitAttackDefinitionId("warrior"), 15),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(booster, GridPosition.FrontBottom, false)
                    .Place(warrior, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem with { Hitpoints = 85 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void BoostInfiniteTest()
    {
        UnitDefinition boosterDefinition = IncreaseDamageDefinition("booster", initiative: 60, level: 4, isInfinite: true, reach: UnitAttackReach.Any);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 10);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2);

        BattleUnit booster = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, boosterDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(boosterDefinition)
            .WithDefinition(warriorDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(booster, GridPosition.FrontBottom)
            .WithAttacker(warrior, GridPosition.FrontCenter)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(booster.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(booster.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("booster")]),
            new BattleUnitAttackedEvent(booster.Id, warrior.Id, new UnitAttackDefinitionId("booster")),
            new BattleUnitAttackHitEvent(booster.Id, warrior.Id, new UnitAttackDefinitionId("booster"), 0),
            new BattleUnitEffectAppliedEvent(booster.Id, warrior.Id, new BattleIncreaseDamageStatusEffect(100, true)),
            new BattleUnitTurnEndingEvent(booster.Id),
            new BattleUnitTurnEndedEvent(booster.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, golem.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, golem.Id, new UnitAttackDefinitionId("warrior"), 20),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(booster, GridPosition.FrontBottom, false)
                    .Place(warrior with { StatusEffects = [new BattleIncreaseDamageStatusEffect(100, true)] }, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void BoostAllTest()
    {
        UnitDefinition boosterDefinition = IncreaseDamageDefinition("booster", initiative: 60, level: 1, isInfinite: false, reach: UnitAttackReach.All);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 10);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2);

        BattleUnit booster = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, boosterDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(boosterDefinition)
            .WithDefinition(warriorDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(booster, GridPosition.FrontBottom)
            .WithAttacker(warrior, GridPosition.FrontCenter)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(booster.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(booster.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("booster")]),
            new BattleUnitAttackedEvent(booster.Id, warrior.Id, new UnitAttackDefinitionId("booster")),
            new BattleUnitAttackHitEvent(booster.Id, warrior.Id, new UnitAttackDefinitionId("booster"), 0),
            new BattleUnitEffectAppliedEvent(booster.Id, warrior.Id, new BattleIncreaseDamageStatusEffect(25, false)),
            new BattleUnitTurnEndingEvent(booster.Id),
            new BattleUnitTurnEndedEvent(booster.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(booster, GridPosition.FrontBottom, false)
                    .Place(warrior with { StatusEffects = [new BattleIncreaseDamageStatusEffect(25, false)] }, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void BoostBattleEndTest()
    {
        UnitDefinition boosterDefinition = IncreaseDamageDefinition("booster", initiative: 60, level: 2, isInfinite: false, reach: UnitAttackReach.Any);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 10);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 10, initiative: 40, damage: 2);

        BattleUnit booster = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, boosterDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(boosterDefinition)
            .WithDefinition(warriorDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(booster, GridPosition.FrontBottom)
            .WithAttacker(warrior, GridPosition.FrontCenter)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(booster.Id, new BattleAttackUnitTarget(warrior.Id)));

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, golem.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, golem.Id, new UnitAttackDefinitionId("warrior"), 15),
            new BattleUnitDiedEvent(golem.Id),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleEndedEvent(BattleSide.Attacker),
            new BattleUnitExperienceGainedEvent(booster.Id, 1),
            new BattleUnitExperienceGainedEvent(warrior.Id, 1)
        ]);

        BattleState expected = engine.State
                .WithUnit(golem with { Hitpoints = 0 })
            with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        Assert.That(engine.State, Is.EqualTo(expected));
    }

    [Test]
    public void BoostEnemyTargetTest()
    {
        UnitDefinition boosterDefinition = IncreaseDamageDefinition("booster", initiative: 60, level: 1, isInfinite: false, reach: UnitAttackReach.Any);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2);

        BattleUnit booster = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, boosterDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(boosterDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(booster, GridPosition.FrontBottom)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(new BattleAttackCommand(booster.Id, new BattleAttackUnitTarget(golem.Id)), BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void AlreadySlowedTargetTest()
    {
        UnitDefinition witchDefinition = ReduceInitiativeDefinition("witch", initiative: 60);
        UnitDefinition hagDefinition = ReduceInitiativeDefinition("hag", initiative: 55);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 10);

        BattleUnit witch = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, witchDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit hag = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, hagDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, warriorDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(witchDefinition)
            .WithDefinition(hagDefinition)
            .WithDefinition(warriorDefinition)
            .WithAttacker(witch, GridPosition.FrontBottom)
            .WithAttacker(hag, GridPosition.FrontCenter)
            .WithDefender(warrior, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(witch.Id, new BattleAttackUnitTarget(warrior.Id)));

        engine.CheckCommandError(
            new BattleAttackCommand(hag.Id, new BattleAttackUnitTarget(warrior.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void LowerDamageOnDisabledTargetTest()
    {
        UnitDefinition witchDefinition = ReduceDamageDefinition("witch", initiative: 60, level: 1);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 10);

        BattleUnit witch = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, witchDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit warrior = new(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Defender, 50, 0, 0, 1, [], [new BattleParalyzeStatusEffect(2)], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(witchDefinition)
            .WithDefinition(warriorDefinition)
            .WithAttacker(witch, GridPosition.FrontBottom)
            .WithDefender(warrior, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(witch.Id, new BattleAttackUnitTarget(warrior.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void BoostRetreatingAllyTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 70, damage: 10);
        UnitDefinition boosterDefinition = IncreaseDamageDefinition("booster", initiative: 60, level: 1, isInfinite: false, reach: UnitAttackReach.Any);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit booster = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, boosterDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(boosterDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(booster, GridPosition.FrontCenter)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleRetreatCommand(warrior.Id));

        engine.CheckCommandError(
            new BattleAttackCommand(booster.Id, new BattleAttackUnitTarget(warrior.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void BoostSelfTest()
    {
        UnitDefinition boosterDefinition = IncreaseDamageDefinition("booster", initiative: 60, level: 1, isInfinite: false, reach: UnitAttackReach.Any);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2);

        BattleUnit booster = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, boosterDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(boosterDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(booster, GridPosition.FrontBottom)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(booster.Id, new BattleAttackUnitTarget(booster.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void BoostHealerTest()
    {
        UnitDefinition boosterDefinition = IncreaseDamageDefinition("booster", initiative: 60, level: 1, isInfinite: false, reach: UnitAttackReach.Any);
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 50, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Heal = 10
            }
        };
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2);

        BattleUnit booster = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, boosterDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, healerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(boosterDefinition)
            .WithDefinition(healerDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(booster, GridPosition.FrontBottom)
            .WithAttacker(healer, GridPosition.FrontCenter)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(booster.Id, new BattleAttackUnitTarget(healer.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void BoostDrainingTargetTest()
    {
        UnitDefinition boosterDefinition = IncreaseDamageDefinition("booster", initiative: 60, level: 1, isInfinite: false, reach: UnitAttackReach.Any);
        UnitDefinition vampireDefinition = TestHelpers.SimpleUnitDefinition("vampire", maxHitpoints: 50, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("vampire"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.DrainLife
            }
        };
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2);

        BattleUnit booster = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, boosterDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit vampire = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, vampireDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(boosterDefinition)
            .WithDefinition(vampireDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(booster, GridPosition.FrontBottom)
            .WithAttacker(vampire, GridPosition.FrontCenter)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(booster.Id, new BattleAttackUnitTarget(vampire.Id)), [
            new BattleUnitAttackEvent(booster.Id, new BattleAttackUnitTarget(vampire.Id), [new UnitAttackDefinitionId("booster")]),
            new BattleUnitAttackedEvent(booster.Id, vampire.Id, new UnitAttackDefinitionId("booster")),
            new BattleUnitAttackHitEvent(booster.Id, vampire.Id, new UnitAttackDefinitionId("booster"), 0),
            new BattleUnitEffectAppliedEvent(booster.Id, vampire.Id, new BattleIncreaseDamageStatusEffect(25, false)),
            new BattleUnitTurnEndingEvent(booster.Id),
            new BattleUnitTurnEndedEvent(booster.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(vampire.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(booster, GridPosition.FrontBottom, false)
                    .Place(vampire with { StatusEffects = [new BattleIncreaseDamageStatusEffect(25, false)] }, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(vampire.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void ReduceDamageSupportTargetTest()
    {
        UnitDefinition witchDefinition = ReduceDamageDefinition("witch", initiative: 60, level: 1);
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 50, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Heal = 10
            }
        };

        BattleUnit witch = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, witchDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, healerDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(witchDefinition)
            .WithDefinition(healerDefinition)
            .WithAttacker(witch, GridPosition.FrontBottom)
            .WithDefender(healer, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(witch.Id, new BattleAttackUnitTarget(healer.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void BoostWeakerTest()
    {
        UnitDefinition strongBoosterDefinition = IncreaseDamageDefinition("strong", initiative: 70, level: 2, isInfinite: false, reach: UnitAttackReach.Any);
        UnitDefinition weakBoosterDefinition = IncreaseDamageDefinition("weak", initiative: 60, level: 1, isInfinite: false, reach: UnitAttackReach.Any);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 10);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2);

        BattleUnit strongBooster = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, strongBoosterDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit weakBooster = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, weakBoosterDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(strongBoosterDefinition)
            .WithDefinition(weakBoosterDefinition)
            .WithDefinition(warriorDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(strongBooster, GridPosition.FrontBottom)
            .WithAttacker(weakBooster, GridPosition.FrontCenter)
            .WithAttacker(warrior, GridPosition.FrontTop)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(strongBooster.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(strongBooster.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("strong")]),
            new BattleUnitAttackedEvent(strongBooster.Id, warrior.Id, new UnitAttackDefinitionId("strong")),
            new BattleUnitAttackHitEvent(strongBooster.Id, warrior.Id, new UnitAttackDefinitionId("strong"), 0),
            new BattleUnitEffectAppliedEvent(strongBooster.Id, warrior.Id, new BattleIncreaseDamageStatusEffect(50, false)),
            new BattleUnitTurnEndingEvent(strongBooster.Id),
            new BattleUnitTurnEndedEvent(strongBooster.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(weakBooster.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommandError(
            new BattleAttackCommand(weakBooster.Id, new BattleAttackUnitTarget(warrior.Id)),
            BattleCommandError.TargetIncorrect);

        _ = engine.DispatchCommand(new BattleSkipCommand(weakBooster.Id));

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, golem.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, golem.Id, new UnitAttackDefinitionId("warrior"), 15),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(strongBooster, GridPosition.FrontBottom, false)
                    .Place(weakBooster, GridPosition.FrontCenter, false)
                    .Place(warrior, GridPosition.FrontTop, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem with { Hitpoints = 85 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void LowerDamageTest()
    {
        UnitDefinition witchDefinition = ReduceDamageDefinition("witch", initiative: 60, level: 1);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 50, damage: 10);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit witch = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, witchDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(witchDefinition)
            .WithDefinition(warriorDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithDefender(witch, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(witch.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(witch.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("witch")]),
            new BattleUnitAttackedEvent(witch.Id, warrior.Id, new UnitAttackDefinitionId("witch")),
            new BattleUnitAttackHitEvent(witch.Id, warrior.Id, new UnitAttackDefinitionId("witch"), 0),
            new BattleUnitEffectAppliedEvent(witch.Id, warrior.Id, new BattleReduceDamageStatusEffect(50, false)),
            new BattleUnitTurnEndingEvent(witch.Id),
            new BattleUnitTurnEndedEvent(witch.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(witch.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(witch.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, witch.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, witch.Id, new UnitAttackDefinitionId("warrior"), 5),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(warrior, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(witch with { Hitpoints = 45 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void LowerDamageLevelTwoTest()
    {
        UnitDefinition witchDefinition = ReduceDamageDefinition("witch", initiative: 60, level: 2);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 50, damage: 10);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit witch = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, witchDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(witchDefinition)
            .WithDefinition(warriorDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithDefender(witch, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(witch.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(witch.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("witch")]),
            new BattleUnitAttackedEvent(witch.Id, warrior.Id, new UnitAttackDefinitionId("witch")),
            new BattleUnitAttackHitEvent(witch.Id, warrior.Id, new UnitAttackDefinitionId("witch"), 0),
            new BattleUnitEffectAppliedEvent(witch.Id, warrior.Id, new BattleReduceDamageStatusEffect(33, false)),
            new BattleUnitTurnEndingEvent(witch.Id),
            new BattleUnitTurnEndedEvent(witch.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(witch.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(witch.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, witch.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, witch.Id, new UnitAttackDefinitionId("warrior"), 7),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(warrior, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(witch with { Hitpoints = 43 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void ReduceInitiativeTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 50, damage: 1);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 100, initiative: 40, damage: 1);
        UnitDefinition witchDefinition = ReduceInitiativeDefinition("witch", initiative: 45);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, scoutDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit witch = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, witchDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(scoutDefinition)
            .WithDefinition(witchDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(scout, GridPosition.FrontCenter)
            .WithDefender(witch, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(witch.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(witch.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, witch.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, witch.Id, new UnitAttackDefinitionId("warrior"), 1),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(witch.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(witch.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("witch")]),
            new BattleUnitAttackedEvent(witch.Id, warrior.Id, new UnitAttackDefinitionId("witch")),
            new BattleUnitAttackHitEvent(witch.Id, warrior.Id, new UnitAttackDefinitionId("witch"), 0),
            new BattleUnitEffectAppliedEvent(witch.Id, warrior.Id, new BattleReduceInitiativeStatusEffect(50, false)),
            new BattleUnitTurnEndingEvent(witch.Id),
            new BattleUnitTurnEndedEvent(witch.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(scout.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(scout.Id, new BattleAttackUnitTarget(witch.Id)), [
            new BattleUnitAttackEvent(scout.Id, new BattleAttackUnitTarget(witch.Id), [new UnitAttackDefinitionId("scout")]),
            new BattleUnitAttackedEvent(scout.Id, witch.Id, new UnitAttackDefinitionId("scout")),
            new BattleUnitAttackHitEvent(scout.Id, witch.Id, new UnitAttackDefinitionId("scout"), 1),
            new BattleUnitTurnEndingEvent(scout.Id),
            new BattleUnitTurnEndedEvent(scout.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(scout.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(warrior, GridPosition.FrontBottom, false)
                    .Place(scout, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(witch with { Hitpoints = 98 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void ReduceInitiativeBeforeTargetTurnTest()
    {
        UnitDefinition witchDefinition = ReduceInitiativeDefinition("witch", initiative: 60);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 50, damage: 1);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 100, initiative: 40, damage: 1);
        UnitDefinition snailDefinition = TestHelpers.SimpleUnitDefinition("snail", maxHitpoints: 100, initiative: 10, damage: 1);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, scoutDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit snail = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, snailDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit witch = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, witchDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(witchDefinition)
            .WithDefinition(warriorDefinition)
            .WithDefinition(scoutDefinition)
            .WithDefinition(snailDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(scout, GridPosition.FrontCenter)
            .WithAttacker(snail, GridPosition.FrontTop)
            .WithDefender(witch, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(witch.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(witch.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("witch")]),
            new BattleUnitAttackedEvent(witch.Id, warrior.Id, new UnitAttackDefinitionId("witch")),
            new BattleUnitAttackHitEvent(witch.Id, warrior.Id, new UnitAttackDefinitionId("witch"), 0),
            new BattleUnitEffectAppliedEvent(witch.Id, warrior.Id, new BattleReduceInitiativeStatusEffect(50, false)),
            new BattleUnitTurnEndingEvent(witch.Id),
            new BattleUnitTurnEndedEvent(witch.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal))
        ]);

        _ = engine.DispatchCommand(new BattleSkipCommand(warrior.Id));

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(warrior with { StatusEffects = [new BattleReduceInitiativeStatusEffect(50, false)] }, GridPosition.FrontBottom, false)
                    .Place(scout, GridPosition.FrontCenter, false)
                    .Place(snail, GridPosition.FrontTop, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(witch, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(scout.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));

        _ = engine.DispatchCommand(new BattleSkipCommand(scout.Id));

        engine.CheckCommand(new BattleSkipCommand(snail.Id), [
            new BattleUnitTurnEndingEvent(snail.Id),
            new BattleUnitTurnEndedEvent(snail.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(scout.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(snail.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal))
        ]);

        _ = engine.DispatchCommand(new BattleSkipCommand(witch.Id));
        _ = engine.DispatchCommand(new BattleSkipCommand(scout.Id));
        _ = engine.DispatchCommand(new BattleSkipCommand(warrior.Id));

        engine.CheckCommand(new BattleSkipCommand(snail.Id), [
            new BattleUnitTurnEndingEvent(snail.Id),
            new BattleUnitTurnEndedEvent(snail.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(3), [new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(scout.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(snail.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(warrior, GridPosition.FrontBottom, false)
                    .Place(scout, GridPosition.FrontCenter, false)
                    .Place(snail, GridPosition.FrontTop, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(witch, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(3)
            }));
    }

    [Test]
    public void CuresBoostedAllyTest()
    {
        UnitDefinition boosterDefinition = IncreaseDamageDefinition("booster", initiative: 60, level: 2, isInfinite: false, reach: UnitAttackReach.Any) with
        {
            SecondaryAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("booster-cure"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Cure,
                Source = UnitAttackSource.Life
            }
        };
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 10);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2);

        BattleUnit booster = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, boosterDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50) with
        {
            StatusEffects = [new BattleIncreaseDamageStatusEffect(50, true), new BattlePoisonStatusEffect(2, 5)]
        };
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(boosterDefinition)
            .WithDefinition(warriorDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(booster, GridPosition.FrontBottom)
            .WithAttacker(warrior, GridPosition.FrontCenter)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(booster.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(booster.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("booster"), new UnitAttackDefinitionId("booster-cure")]),
            new BattleUnitAttackedEvent(booster.Id, warrior.Id, new UnitAttackDefinitionId("booster-cure")),
            new BattleUnitAttackHitEvent(booster.Id, warrior.Id, new UnitAttackDefinitionId("booster-cure"), 0),
            new BattleUnitCuredEvent(warrior.Id),
            new BattleUnitTurnEndingEvent(booster.Id),
            new BattleUnitTurnEndedEvent(booster.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(booster, GridPosition.FrontBottom, false)
                    .Place(warrior with { StatusEffects = [new BattleIncreaseDamageStatusEffect(50, true), new BattleCureStatusEffect()] }, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void SlowAgainBeforeTargetTurnTest()
    {
        UnitDefinition witchDefinition = ReduceInitiativeDefinition("witch", initiative: 60);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 100, initiative: 50, damage: 1);
        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 100, initiative: 40, damage: 1);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, scoutDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit witch = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, witchDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(witchDefinition)
            .WithDefinition(warriorDefinition)
            .WithDefinition(scoutDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithAttacker(scout, GridPosition.FrontCenter)
            .WithDefender(witch, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(witch.Id, new BattleAttackUnitTarget(warrior.Id)));
        _ = engine.DispatchCommand(new BattleSkipCommand(warrior.Id));

        engine.CheckCommand(new BattleSkipCommand(scout.Id), [
            new BattleUnitTurnEndingEvent(scout.Id),
            new BattleUnitTurnEndedEvent(scout.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(scout.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(witch.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(witch.Id, new BattleAttackUnitTarget(warrior.Id)), [
            new BattleUnitAttackEvent(witch.Id, new BattleAttackUnitTarget(warrior.Id), [new UnitAttackDefinitionId("witch")]),
            new BattleUnitAttackedEvent(witch.Id, warrior.Id, new UnitAttackDefinitionId("witch")),
            new BattleUnitAttackHitEvent(witch.Id, warrior.Id, new UnitAttackDefinitionId("witch"), 0),
            new BattleUnitEffectAppliedEvent(witch.Id, warrior.Id, new BattleReduceInitiativeStatusEffect(50, false)),
            new BattleUnitTurnEndingEvent(witch.Id),
            new BattleUnitTurnEndedEvent(witch.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(scout.Id, BattleUnitTurnType.Normal))
        ]);
    }
}
