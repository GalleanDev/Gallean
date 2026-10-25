namespace Gallean.Engine.Tests;

public sealed class BattleEngineShatterAttackTests
{
    private static UnitDefinition ShattererDefinition(string id, int initiative, int damage)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 50, initiative: initiative, damage: damage) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId(id), initiative: initiative, damage: damage, accuracy: 0, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.ReduceArmor
            }
        };
    }

    [Test]
    public void RetreatingTargetTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 60, damage: 10) with
        {
            Armor = 50
        };
        UnitDefinition shattererDefinition = ShattererDefinition("shatterer", initiative: 50, damage: 20);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit shatterer = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, shattererDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(warriorDefinition)
            .WithDefinition(shattererDefinition)
            .WithAttacker(warrior, GridPosition.FrontBottom)
            .WithDefender(shatterer, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleRetreatCommand(warrior.Id));

        engine.CheckCommandError(
            new BattleAttackCommand(shatterer.Id, new BattleAttackUnitTarget(warrior.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void ReducesArmorTest()
    {
        UnitDefinition shattererDefinition = ShattererDefinition("shatterer", initiative: 60, damage: 20);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 10);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2) with
        {
            Armor = 50
        };

        BattleUnit shatterer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, shattererDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(shattererDefinition)
            .WithDefinition(warriorDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(shatterer, GridPosition.FrontBottom)
            .WithAttacker(warrior, GridPosition.FrontCenter)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(shatterer.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(shatterer.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("shatterer")]),
            new BattleUnitAttackedEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer")),
            new BattleUnitAttackHitEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer"), 0),
            new BattleUnitEffectAppliedEvent(shatterer.Id, golem.Id, new BattleShatterArmorStatusEffect(20)),
            new BattleUnitTurnEndingEvent(shatterer.Id),
            new BattleUnitTurnEndedEvent(shatterer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, golem.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, golem.Id, new UnitAttackDefinitionId("warrior"), 7),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(shatterer, GridPosition.FrontBottom, false)
                    .Place(warrior, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem with { Hitpoints = 93, StatusEffects = [new BattleShatterArmorStatusEffect(20)] }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void RemainingArmorCapTest()
    {
        UnitDefinition shattererDefinition = ShattererDefinition("shatterer", initiative: 60, damage: 80);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2) with
        {
            Armor = 50
        };

        BattleUnit shatterer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, shattererDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(shattererDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(shatterer, GridPosition.FrontBottom)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(shatterer.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(shatterer.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("shatterer")]),
            new BattleUnitAttackedEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer")),
            new BattleUnitAttackHitEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer"), 0),
            new BattleUnitEffectAppliedEvent(shatterer.Id, golem.Id, new BattleShatterArmorStatusEffect(50)),
            new BattleUnitTurnEndingEvent(shatterer.Id),
            new BattleUnitTurnEndedEvent(shatterer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(golem.Id, new BattleAttackUnitTarget(shatterer.Id)), [
            new BattleUnitAttackEvent(golem.Id, new BattleAttackUnitTarget(shatterer.Id), [new UnitAttackDefinitionId("golem")]),
            new BattleUnitAttackedEvent(golem.Id, shatterer.Id, new UnitAttackDefinitionId("golem")),
            new BattleUnitAttackHitEvent(golem.Id, shatterer.Id, new UnitAttackDefinitionId("golem"), 2),
            new BattleUnitTurnEndingEvent(golem.Id),
            new BattleUnitTurnEndedEvent(golem.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(shatterer.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(shatterer.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommandError(
            new BattleAttackCommand(shatterer.Id, new BattleAttackUnitTarget(golem.Id)),
            BattleCommandError.TargetIncorrect);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(shatterer with { Hitpoints = 48 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem with { StatusEffects = [new BattleShatterArmorStatusEffect(50)] }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(shatterer.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void TotalCapTest()
    {
        UnitDefinition shattererDefinition = ShattererDefinition("shatterer", initiative: 60, damage: 40);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2) with
        {
            Armor = 90
        };

        BattleUnit shatterer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, shattererDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(shattererDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(shatterer, GridPosition.FrontBottom)
            .WithDefender(golem, GridPosition.FrontBottom)
            .WithGlobals(new GlobalVariables { BattleInitiativeVariance = 0, BattleDamageVariance = 0, ShatterArmorTotalCap = 60 })
            .Build();

        engine.CheckCommand(new BattleAttackCommand(shatterer.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(shatterer.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("shatterer")]),
            new BattleUnitAttackedEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer")),
            new BattleUnitAttackHitEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer"), 0),
            new BattleUnitEffectAppliedEvent(shatterer.Id, golem.Id, new BattleShatterArmorStatusEffect(40)),
            new BattleUnitTurnEndingEvent(shatterer.Id),
            new BattleUnitTurnEndedEvent(shatterer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(golem.Id, new BattleAttackUnitTarget(shatterer.Id)), [
            new BattleUnitAttackEvent(golem.Id, new BattleAttackUnitTarget(shatterer.Id), [new UnitAttackDefinitionId("golem")]),
            new BattleUnitAttackedEvent(golem.Id, shatterer.Id, new UnitAttackDefinitionId("golem")),
            new BattleUnitAttackHitEvent(golem.Id, shatterer.Id, new UnitAttackDefinitionId("golem"), 2),
            new BattleUnitTurnEndingEvent(golem.Id),
            new BattleUnitTurnEndedEvent(golem.Id),
            new BattleRoundEndedEvent(),
            new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(shatterer.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal)]),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(shatterer.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(shatterer.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(shatterer.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("shatterer")]),
            new BattleUnitAttackedEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer")),
            new BattleUnitAttackHitEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer"), 0),
            new BattleUnitEffectAppliedEvent(shatterer.Id, golem.Id, new BattleShatterArmorStatusEffect(20)),
            new BattleUnitTurnEndingEvent(shatterer.Id),
            new BattleUnitTurnEndedEvent(shatterer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(shatterer with { Hitpoints = 48 }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem with { StatusEffects = [new BattleShatterArmorStatusEffect(60)] }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void PerHitCapTest()
    {
        UnitDefinition shattererDefinition = ShattererDefinition("shatterer", initiative: 60, damage: 120);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2) with
        {
            Armor = 90
        };

        BattleUnit shatterer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, shattererDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(shattererDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(shatterer, GridPosition.FrontBottom)
            .WithDefender(golem, GridPosition.FrontBottom)
            .WithGlobals(new GlobalVariables { BattleInitiativeVariance = 0, BattleDamageVariance = 0, ShatterArmorPerHitCap = 50 })
            .Build();

        engine.CheckCommand(new BattleAttackCommand(shatterer.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(shatterer.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("shatterer")]),
            new BattleUnitAttackedEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer")),
            new BattleUnitAttackHitEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer"), 0),
            new BattleUnitEffectAppliedEvent(shatterer.Id, golem.Id, new BattleShatterArmorStatusEffect(50)),
            new BattleUnitTurnEndingEvent(shatterer.Id),
            new BattleUnitTurnEndedEvent(shatterer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(shatterer, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem with { StatusEffects = [new BattleShatterArmorStatusEffect(50)] }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void NegativeAddsArmorTest()
    {
        UnitDefinition shattererDefinition = ShattererDefinition("shatterer", initiative: 60, damage: -30);
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 50, initiative: 50, damage: 10);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2) with
        {
            Armor = 50
        };

        BattleUnit shatterer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, shattererDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(shattererDefinition)
            .WithDefinition(warriorDefinition)
            .WithDefinition(golemDefinition)
            .WithAttacker(shatterer, GridPosition.FrontBottom)
            .WithAttacker(warrior, GridPosition.FrontCenter)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(shatterer.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(shatterer.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("shatterer")]),
            new BattleUnitAttackedEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer")),
            new BattleUnitAttackHitEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer"), 0),
            new BattleUnitEffectAppliedEvent(shatterer.Id, golem.Id, new BattleShatterArmorStatusEffect(-30)),
            new BattleUnitTurnEndingEvent(shatterer.Id),
            new BattleUnitTurnEndedEvent(shatterer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(warrior.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(warrior.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(warrior.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("warrior")]),
            new BattleUnitAttackedEvent(warrior.Id, golem.Id, new UnitAttackDefinitionId("warrior")),
            new BattleUnitAttackHitEvent(warrior.Id, golem.Id, new UnitAttackDefinitionId("warrior"), 2),
            new BattleUnitTurnEndingEvent(warrior.Id),
            new BattleUnitTurnEndedEvent(warrior.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(shatterer, GridPosition.FrontBottom, false)
                    .Place(warrior, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem with { Hitpoints = 98, StatusEffects = [new BattleShatterArmorStatusEffect(-30)] }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void AccuracyTest()
    {
        UnitDefinition shattererDefinition = ShattererDefinition("shatterer", initiative: 60, damage: 20);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2) with
        {
            Armor = 50
        };

        BattleUnit shatterer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, shattererDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(shattererDefinition)
            .WithDefinition(golemDefinition)
            .WithRandom(new TestSequenceRandomProvider())
            .WithAttacker(shatterer, GridPosition.FrontBottom)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(shatterer.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(shatterer.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("shatterer")]),
            new BattleUnitAttackedEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer")),
            new BattleUnitAttackHitEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer"), 0),
            new BattleUnitEffectAppliedEvent(shatterer.Id, golem.Id, new BattleShatterArmorStatusEffect(20)),
            new BattleUnitTurnEndingEvent(shatterer.Id),
            new BattleUnitTurnEndedEvent(shatterer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(shatterer, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem with { StatusEffects = [new BattleShatterArmorStatusEffect(20)] }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void FortifiedRemainingArmorCapTest()
    {
        UnitDefinition shattererDefinition = ShattererDefinition("shatterer", initiative: 60, damage: 80);
        UnitDefinition golemDefinition = TestHelpers.SimpleUnitDefinition("golem", maxHitpoints: 100, initiative: 40, damage: 2) with
        {
            Armor = 20
        };

        UnitModifierDefinition protection = TestHelpers.SimpleModifier("protection") with { Armor = new UnitStatModifier(10, 0) };

        BattleUnit shatterer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, shattererDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit golem = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, golemDefinition.Id, BattleSide.Defender, hitpoints: 100) with
        {
            Modifiers = [UnitModifier.FromFortification(protection.Id)],
            StatusEffects = [new BattleFortificationStatusEffect(10)]
        };

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(shattererDefinition)
            .WithDefinition(golemDefinition)
            .WithDefinition(protection)
            .WithAttacker(shatterer, GridPosition.FrontBottom)
            .WithDefender(golem, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(shatterer.Id, new BattleAttackUnitTarget(golem.Id)), [
            new BattleUnitAttackEvent(shatterer.Id, new BattleAttackUnitTarget(golem.Id), [new UnitAttackDefinitionId("shatterer")]),
            new BattleUnitAttackedEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer")),
            new BattleUnitAttackHitEvent(shatterer.Id, golem.Id, new UnitAttackDefinitionId("shatterer"), 0),
            new BattleUnitEffectAppliedEvent(shatterer.Id, golem.Id, new BattleShatterArmorStatusEffect(30)),
            new BattleUnitTurnEndingEvent(shatterer.Id),
            new BattleUnitTurnEndedEvent(shatterer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(shatterer, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(golem with { StatusEffects = [new BattleFortificationStatusEffect(10), new BattleShatterArmorStatusEffect(30)] }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(golem.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }
}
