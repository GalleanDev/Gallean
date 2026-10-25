namespace Gallean.Engine.Tests;

public sealed class BattleEngineAttackReachTests
{
    [Test]
    public void EnemyFrontlineBlocksMeleeTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("warrior"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition defenderBackDefinition = TestHelpers.SimpleUnitDefinition("enemy_back", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("enemy_back"), initiative: 40, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition defenderFrontDefinition = TestHelpers.SimpleUnitDefinition("enemy_front", maxHitpoints: 10, initiative: 30, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("enemy_front"), initiative: 30, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit defenderBack = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderBackDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleUnit defenderFront = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderFrontDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderBackDefinition)
            .WithDefinition(defenderFrontDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defenderBack, new GridPosition(1, 0))
            .WithDefender(defenderFront, new GridPosition(0, 1))
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defenderBack.Id)),
            BattleCommandError.TargetNotReachable
        );
    }

    [Test]
    public void FlankBlocksMeleeTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("warrior_bottom", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("warrior_bottom"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition defenderTopDefinition = TestHelpers.SimpleUnitDefinition("enemy_top", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("enemy_top"), initiative: 40, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition defenderCenterDefinition = TestHelpers.SimpleUnitDefinition("enemy_center", maxHitpoints: 10, initiative: 30, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("enemy_center"), initiative: 30, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit defenderTop = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderTopDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleUnit defenderCenter = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderCenterDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderTopDefinition)
            .WithDefinition(defenderCenterDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defenderTop, new GridPosition(0, 2))
            .WithDefender(defenderCenter, new GridPosition(0, 1))
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defenderTop.Id)),
            BattleCommandError.TargetNotReachable
        );
    }

    [Test]
    public void OppositeFlankBlocksMeleeTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("warrior_bottom", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("warrior_bottom"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition defenderTopDefinition = TestHelpers.SimpleUnitDefinition("enemy_top", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("enemy_top"), initiative: 40, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition defenderBottomDefinition = TestHelpers.SimpleUnitDefinition("enemy_bottom", maxHitpoints: 10, initiative: 30, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("enemy_bottom"), initiative: 30, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit defenderTop = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderTopDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleUnit defenderBottom = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderBottomDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderTopDefinition)
            .WithDefinition(defenderBottomDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defenderTop, new GridPosition(0, 2))
            .WithDefender(defenderBottom, new GridPosition(0, 0))
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defenderTop.Id)),
            BattleCommandError.TargetNotReachable
        );
    }

    [Test]
    public void OwnFrontlineBlocksMeleeTest()
    {
        UnitDefinition attackerBackDefinition = TestHelpers.SimpleUnitDefinition("warrior_back", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("warrior_back"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition attackerFrontDefinition = TestHelpers.SimpleUnitDefinition("warrior_front", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("warrior_front"), initiative: 40, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 10, initiative: 30, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("enemy"), initiative: 30, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        BattleUnit attackerBack = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerBackDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit attackerFront = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerFrontDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerBackDefinition)
            .WithDefinition(attackerFrontDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attackerBack, new GridPosition(1, 0))
            .WithAttacker(attackerFront, new GridPosition(0, 1))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attackerBack.Id, new BattleAttackUnitTarget(defender.Id)),
            BattleCommandError.TargetNotReachable
        );
    }

    [Test]
    public void MeleeReachFlankClearTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("warrior_bottom", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("warrior_bottom"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("enemy_top", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("enemy_top"), initiative: 40, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 2))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
            [
                new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [new UnitAttackDefinitionId("warrior_bottom")]),
                new BattleUnitAttackedEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("warrior_bottom")),
                new BattleUnitAttackHitEvent(attacker.Id, defender.Id, new UnitAttackDefinitionId("warrior_bottom"), 5),
                new BattleUnitTurnEndingEvent(attacker.Id),
                new BattleUnitTurnEndedEvent(attacker.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
            ]
        );

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender with { Hitpoints = 5 }, new GridPosition(0, 2), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                DefenderGrid = expectedDefenderGrid,
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void ReachConstraintTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("melee_warrior", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("melee_warrior"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        UnitDefinition defenderFrontDefinition = TestHelpers.SimpleUnitDefinition("front_scout", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitDefinition defenderBackDefinition = TestHelpers.SimpleUnitDefinition("back_mage", maxHitpoints: 10, initiative: 30, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);

        BattleUnit defenderFront = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderFrontDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleUnit defenderBack = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderBackDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderFrontDefinition)
            .WithDefinition(defenderBackDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defenderFront, new GridPosition(0, 0))
            .WithDefender(defenderBack, new GridPosition(1, 0))
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defenderBack.Id)),
            BattleCommandError.TargetNotReachable
        );

        engine.CheckCommand(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defenderFront.Id)),
            [
                new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defenderFront.Id), [new UnitAttackDefinitionId("melee_warrior")]),
                new BattleUnitAttackedEvent(attacker.Id, defenderFront.Id, new UnitAttackDefinitionId("melee_warrior")),
                new BattleUnitAttackHitEvent(attacker.Id, defenderFront.Id, new UnitAttackDefinitionId("melee_warrior"), 10),
                new BattleUnitDiedEvent(UnitId: defenderFront.Id),
                new BattleUnitTurnEndingEvent(attacker.Id),
                new BattleUnitTurnEndedEvent(attacker.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(defenderBack.Id, BattleUnitTurnType.Normal))
            ]
        );

        engine.CheckCommand(
            new BattleSkipCommand(defenderBack.Id),
            [
                new BattleUnitTurnSkippedEvent(defenderBack.Id),
                new BattleUnitTurnEndingEvent(defenderBack.Id),
                new BattleUnitTurnEndedEvent(defenderBack.Id),
                new BattleRoundEndedEvent(),
                new BattleRoundStartedEvent(BattleRound.Normal(2), [new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defenderBack.Id, BattleUnitTurnType.Normal)]),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal))
            ]
        );

        engine.CheckCommand(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defenderBack.Id)),
            [
                new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defenderBack.Id), [new UnitAttackDefinitionId("melee_warrior")]),
                new BattleUnitAttackedEvent(attacker.Id, defenderBack.Id, new UnitAttackDefinitionId("melee_warrior")),
                new BattleUnitAttackHitEvent(attacker.Id, defenderBack.Id, new UnitAttackDefinitionId("melee_warrior"), 10),
                new BattleUnitDiedEvent(UnitId: defenderBack.Id),
                new BattleUnitTurnEndingEvent(attacker.Id),
                new BattleRoundEndedEvent(),
                new BattleEndedEvent(WinningSide: BattleSide.Attacker),
                new BattleUnitExperienceGainedEvent(attacker.Id, 1)
            ]
        );

        UnitGrid<BattleUnit> expectedDefenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defenderFront with { Hitpoints = 0 }, new GridPosition(0, 0), false)
            .Place(defenderBack with { Hitpoints = 0 }, new GridPosition(1, 0), false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                DefenderGrid = expectedDefenderGrid,
                CurrentUnitTurn = new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(2),
                Outcome = new BattleOutcome(BattleSide.Attacker)
            }));
    }

    [Test]
    public void ReachAllTest()
    {
        UnitDefinition wizardDefinition = TestHelpers.SimpleUnitDefinition("wizard", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("wizard"), 50, 5, accuracy: 100, reach: UnitAttackReach.All),
            AttacksTwice = false
        };
        UnitDefinition firstDefenderDefinition = TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitDefinition secondDefenderDefinition = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 10, initiative: 30, damage: 5);

        BattleUnit wizard = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, wizardDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit firstDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, firstDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);
        BattleUnit secondDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, secondDefenderDefinition.Id, BattleSide.Defender, hitpoints: 10);

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
            new BattleUnitAttackHitEvent(wizard.Id, firstDefender.Id, new UnitAttackDefinitionId("wizard"), 5),
            new BattleUnitAttackedEvent(wizard.Id, secondDefender.Id, new UnitAttackDefinitionId("wizard")),
            new BattleUnitAttackHitEvent(wizard.Id, secondDefender.Id, new UnitAttackDefinitionId("wizard"), 5),
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
                    .Place(firstDefender with { Hitpoints = 5 }, GridPosition.FrontBottom, false)
                    .Place(secondDefender with { Hitpoints = 5 }, GridPosition.FrontCenter, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(firstDefender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }
}
