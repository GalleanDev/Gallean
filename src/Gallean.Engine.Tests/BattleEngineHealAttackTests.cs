namespace Gallean.Engine.Tests;

public sealed class BattleEngineHealAttackTests
{
    [Test]
    public void HealsWoundedAllyTest()
    {
        UnitDefinition clericDefinition = TestHelpers.SimpleUnitDefinition("cleric", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("cleric"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Heal = 30
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(clericDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(cleric, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(cleric.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(cleric.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("cleric")]),
            new BattleUnitAttackedEvent(cleric.Id, ally.Id, new UnitAttackDefinitionId("cleric")),
            new BattleUnitAttackHitEvent(cleric.Id, ally.Id, new UnitAttackDefinitionId("cleric"), 0),
            new BattleUnitHealedEvent(ally.Id, 80),
            new BattleUnitTurnEndingEvent(cleric.Id),
            new BattleUnitTurnEndedEvent(cleric.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(cleric, GridPosition.FrontBottom, false)
                    .Place(ally with { Hitpoints = 80 }, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void DoesNotExceedMaxHitpointsTest()
    {
        UnitDefinition clericDefinition = TestHelpers.SimpleUnitDefinition("cleric", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("cleric"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Heal = 30
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 90);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(clericDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(cleric, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(cleric.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(cleric.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("cleric")]),
            new BattleUnitAttackedEvent(cleric.Id, ally.Id, new UnitAttackDefinitionId("cleric")),
            new BattleUnitAttackHitEvent(cleric.Id, ally.Id, new UnitAttackDefinitionId("cleric"), 0),
            new BattleUnitHealedEvent(ally.Id, 100),
            new BattleUnitTurnEndingEvent(cleric.Id),
            new BattleUnitTurnEndedEvent(cleric.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(cleric, GridPosition.FrontBottom, false)
                    .Place(ally with { Hitpoints = 100 }, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void AllReachHealTest()
    {
        UnitDefinition clericDefinition = TestHelpers.SimpleUnitDefinition("cleric", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("cleric"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Heal,
                Heal = 30
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 70);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(clericDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(cleric, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(cleric.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(cleric.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("cleric")]),
            new BattleUnitAttackedEvent(cleric.Id, cleric.Id, new UnitAttackDefinitionId("cleric")),
            new BattleUnitAttackHitEvent(cleric.Id, cleric.Id, new UnitAttackDefinitionId("cleric"), 0),
            new BattleUnitHealedEvent(cleric.Id, 100),
            new BattleUnitAttackedEvent(cleric.Id, ally.Id, new UnitAttackDefinitionId("cleric")),
            new BattleUnitAttackHitEvent(cleric.Id, ally.Id, new UnitAttackDefinitionId("cleric"), 0),
            new BattleUnitHealedEvent(ally.Id, 80),
            new BattleUnitTurnEndingEvent(cleric.Id),
            new BattleUnitTurnEndedEvent(cleric.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(cleric with { Hitpoints = 100 }, GridPosition.FrontBottom, false)
                    .Place(ally with { Hitpoints = 80 }, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void CannotHealEnemyTest()
    {
        UnitDefinition clericDefinition = TestHelpers.SimpleUnitDefinition("cleric", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("cleric"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Heal = 30
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 50);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(clericDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(cleric, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(cleric.Id, new BattleAttackUnitTarget(defender.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void FullHitpointsTargetTest()
    {
        UnitDefinition clericDefinition = TestHelpers.SimpleUnitDefinition("cleric", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("cleric"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Heal = 30
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(clericDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(cleric, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(cleric.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetNotHealable);
    }

    [Test]
    public void AllReachHealFullHitpointsTargetTest()
    {
        UnitDefinition clericDefinition = TestHelpers.SimpleUnitDefinition("cleric", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("cleric"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Heal,
                Heal = 30
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(clericDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(cleric, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(cleric.Id, new BattleAttackUnitTarget(cleric.Id)), [
            new BattleUnitAttackEvent(cleric.Id, new BattleAttackUnitTarget(cleric.Id), [new UnitAttackDefinitionId("cleric")]),
            new BattleUnitAttackedEvent(cleric.Id, ally.Id, new UnitAttackDefinitionId("cleric")),
            new BattleUnitAttackHitEvent(cleric.Id, ally.Id, new UnitAttackDefinitionId("cleric"), 0),
            new BattleUnitHealedEvent(ally.Id, 80),
            new BattleUnitTurnEndingEvent(cleric.Id),
            new BattleUnitTurnEndedEvent(cleric.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(cleric, GridPosition.FrontBottom, false)
                    .Place(ally with { Hitpoints = 80 }, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void AllReachHealFullHitpointsStackTest()
    {
        UnitDefinition clericDefinition = TestHelpers.SimpleUnitDefinition("cleric", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("cleric"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Heal,
                Heal = 30
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit cleric = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, clericDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(clericDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(cleric, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(cleric.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(cleric.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("cleric")]),
            new BattleUnitTurnEndingEvent(cleric.Id),
            new BattleUnitTurnEndedEvent(cleric.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(cleric, GridPosition.FrontBottom, false)
                    .Place(ally, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }
}
