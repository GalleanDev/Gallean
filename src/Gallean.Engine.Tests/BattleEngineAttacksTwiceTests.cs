namespace Gallean.Engine.Tests;

public sealed class BattleEngineAttacksTwiceTests
{
    [Test]
    public void AttacksTwiceTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 50, damage: 4) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), 50, 4, accuracy: 100, reach: UnitAttackReach.Any),
            AttacksTwice = true
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
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
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 4),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.SecondAttack))
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
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.SecondAttack),
                TurnQueue = [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(1)
            }));

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 4),
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
                    .Place(defender with { Hitpoints = 2 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void KillsTargetTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 50, damage: 8) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), 50, 8, accuracy: 100, reach: UnitAttackReach.Any),
            AttacksTwice = true
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 6, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 6);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 8),
            new BattleUnitDiedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
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
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1),
                Outcome = new BattleOutcome(BattleSide.Attacker)
            }));
    }

    [Test]
    public void AttacksTwiceReachAllTest()
    {
        UnitDefinition wizardDefinition = TestHelpers.SimpleUnitDefinition("wizard", maxHitpoints: 30, initiative: 50, damage: 3) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("wizard"), 50, 3, accuracy: 100, reach: UnitAttackReach.All),
            AttacksTwice = true
        };
        UnitDefinition firstDefenderDefinition = TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 20, initiative: 40, damage: 5);
        UnitDefinition secondDefenderDefinition = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 20, initiative: 30, damage: 5);

        BattleUnit wizard = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, wizardDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit firstDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, firstDefenderDefinition.Id, BattleSide.Defender, hitpoints: 20);
        BattleUnit secondDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, secondDefenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(wizardDefinition)
            .WithDefinition(firstDefenderDefinition)
            .WithDefinition(secondDefenderDefinition)
            .WithAttacker(wizard, GridPosition.FrontBottom)
            .WithDefender(firstDefender, GridPosition.FrontBottom)
            .WithDefender(secondDefender, GridPosition.FrontCenter)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(wizard.Id, new BattleAttackUnitTarget(firstDefender.Id)), [
            new BattleUnitAttackEvent(wizard.Id, new BattleAttackUnitTarget(firstDefender.Id), [new UnitAttackDefinitionId("wizard")]),
            new BattleUnitAttackedEvent(wizard.Id, firstDefender.Id, new UnitAttackDefinitionId("wizard")),
            new BattleUnitAttackHitEvent(wizard.Id, firstDefender.Id, new UnitAttackDefinitionId("wizard"), 3),
            new BattleUnitAttackedEvent(wizard.Id, secondDefender.Id, new UnitAttackDefinitionId("wizard")),
            new BattleUnitAttackHitEvent(wizard.Id, secondDefender.Id, new UnitAttackDefinitionId("wizard"), 3),
            new BattleUnitTurnEndingEvent(wizard.Id),
            new BattleUnitTurnEndedEvent(wizard.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(wizard.Id, BattleUnitTurnType.SecondAttack))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(wizard, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(firstDefender with { Hitpoints = 17 }, GridPosition.FrontBottom, false)
                    .Place(secondDefender with { Hitpoints = 17 }, GridPosition.FrontCenter, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(wizard.Id, BattleUnitTurnType.SecondAttack),
                CurrentRound = BattleRound.Normal(1)
            }));

        engine.CheckCommand(new BattleAttackCommand(wizard.Id, new BattleAttackUnitTarget(firstDefender.Id)), [
            new BattleUnitAttackEvent(wizard.Id, new BattleAttackUnitTarget(firstDefender.Id), [new UnitAttackDefinitionId("wizard")]),
            new BattleUnitAttackedEvent(wizard.Id, firstDefender.Id, new UnitAttackDefinitionId("wizard")),
            new BattleUnitAttackHitEvent(wizard.Id, firstDefender.Id, new UnitAttackDefinitionId("wizard"), 3),
            new BattleUnitAttackedEvent(wizard.Id, secondDefender.Id, new UnitAttackDefinitionId("wizard")),
            new BattleUnitAttackHitEvent(wizard.Id, secondDefender.Id, new UnitAttackDefinitionId("wizard"), 3),
            new BattleUnitTurnEndingEvent(wizard.Id),
            new BattleUnitTurnEndedEvent(wizard.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(firstDefender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(wizard, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(firstDefender with { Hitpoints = 14 }, GridPosition.FrontBottom, false)
                    .Place(secondDefender with { Hitpoints = 14 }, GridPosition.FrontCenter, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(firstDefender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void DifferentTargetsTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), 50, 5, accuracy: 100, reach: UnitAttackReach.Any),
            AttacksTwice = true
        };
        UnitDefinition firstDefenderDefinition = TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitDefinition secondDefenderDefinition = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 10, initiative: 30, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit firstDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, firstDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);
        BattleUnit secondDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, secondDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(firstDefenderDefinition)
            .WithDefinition(secondDefenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(firstDefender, GridPosition.FrontBottom)
            .WithDefender(secondDefender, GridPosition.FrontCenter)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(firstDefender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(firstDefender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, firstDefender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, firstDefender.Id, new UnitAttackDefinitionId("attacker"), 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.SecondAttack))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(firstDefender with { Hitpoints = 5 }, GridPosition.FrontBottom, false)
                    .Place(secondDefender, GridPosition.FrontCenter, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.SecondAttack),
                CurrentRound = BattleRound.Normal(1)
            }));

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(secondDefender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(secondDefender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, secondDefender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, secondDefender.Id, new UnitAttackDefinitionId("attacker"), 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(firstDefender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(firstDefender with { Hitpoints = 5 }, GridPosition.FrontBottom, false)
                    .Place(secondDefender with { Hitpoints = 5 }, GridPosition.FrontCenter, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(firstDefender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void SkipsExtraAttackTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), 50, 5, accuracy: 100, reach: UnitAttackReach.Any),
            AttacksTwice = true
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
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
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.SecondAttack))
        ]);

        engine.CheckCommand(new BattleSkipCommand(attacker.Id), [
            new BattleUnitTurnSkippedEvent(attacker.Id),
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
                    .Place(defender with { Hitpoints = 5 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void SecondMoveTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), 50, 5, accuracy: 100, reach: UnitAttackReach.Any),
            AttacksTwice = true
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleDefendCommand(attacker.Id), [
            new BattleUnitDefendedEvent(attacker.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ], dryRun: true);

        engine.CheckCommand(new BattleWaitCommand(attacker.Id), [
            new BattleUnitWaitedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ], dryRun: true);

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.SecondAttack))
        ]);

        engine.CheckCommandError(new BattleWaitCommand(attacker.Id), BattleCommandError.ActionNotAvailable);

        engine.CheckCommand(new BattleDefendCommand(attacker.Id), [
            new BattleUnitDefendedEvent(attacker.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ], dryRun: true);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 15 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.SecondAttack),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void DefendsTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            AttacksTwice = true
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleDefendCommand(attacker.Id), [
            new BattleUnitDefendedEvent(attacker.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { StatusEffects = [new BattleDefendStatusEffect()] }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                TurnQueue = [],
                CurrentRound = BattleRound.Normal(1)
            }));

        engine.CheckCommand(new BattleSkipCommand(defender.Id), [
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
                TurnQueue = [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
                CurrentRound = BattleRound.Normal(2)
            }));
    }

    [Test]
    public void WaitsTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), 50, 5, accuracy: 100, reach: UnitAttackReach.Any),
            AttacksTwice = true
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleWaitCommand(attacker.Id), [
            new BattleUnitWaitedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                TurnQueue = [],
                WaitingQueue = [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Resumed)],
                CurrentRound = BattleRound.Normal(1)
            }));

        engine.CheckCommand(new BattleSkipCommand(defender.Id), [
            new BattleUnitTurnSkippedEvent(defender.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Resumed))
        ]);

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("attacker")]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker")),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("attacker"), 5),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.SecondAttack))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 15 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.SecondAttack),
                TurnQueue = [],
                WaitingQueue = [],
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void DefendsOnSecondAttackTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), 50, 5, accuracy: 100, reach: UnitAttackReach.Any),
            AttacksTwice = true
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        _ = engine.DispatchCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)));

        engine.CheckCommand(new BattleDefendCommand(attacker.Id), [
            new BattleUnitDefendedEvent(attacker.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker with { StatusEffects = [new BattleDefendStatusEffect()] }, GridPosition.FrontBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 15 }, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1),
                TurnQueue = []
            }));
    }
}
