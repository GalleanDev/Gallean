namespace Gallean.Engine.Tests;

public sealed class BattleEngineCureAttackTests
{
    [Test]
    public void RemovesNegativeEffectTest()
    {
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Cure
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, healerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = new(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, 80, 0, 0, 1, [], [new BattlePoisonStatusEffect(3, 10)], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(healerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(healer, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(healer.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(healer.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("healer")]),
            new BattleUnitAttackedEvent(healer.Id, ally.Id, new UnitAttackDefinitionId("healer")),
            new BattleUnitAttackHitEvent(healer.Id, ally.Id, new UnitAttackDefinitionId("healer"), 0),
            new BattleUnitCuredEvent(ally.Id),
            new BattleUnitTurnEndingEvent(healer.Id),
            new BattleUnitTurnEndedEvent(healer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(healer, GridPosition.FrontBottom, false)
                    .Place(ally with { StatusEffects = [new BattleCureStatusEffect()] }, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void AllReachCureTest()
    {
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Cure
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, healerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = new(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, 80, 0, 0, 1, [], [new BattleParalyzeStatusEffect(1)], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(healerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(healer, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(healer.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(healer.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("healer")]),
            new BattleUnitAttackedEvent(healer.Id, ally.Id, new UnitAttackDefinitionId("healer")),
            new BattleUnitAttackHitEvent(healer.Id, ally.Id, new UnitAttackDefinitionId("healer"), 0),
            new BattleUnitCuredEvent(ally.Id),
            new BattleUnitTurnEndingEvent(healer.Id),
            new BattleUnitTurnEndedEvent(healer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(healer, GridPosition.FrontBottom, false)
                    .Place(ally with { StatusEffects = [new BattleCureStatusEffect()] }, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void CleanAllyTest()
    {
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Cure
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, healerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 80);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(healerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(healer, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(healer.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetNotCurable);
    }

    [Test]
    public void CannotCureEnemyTest()
    {
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Cure
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, healerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = new(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, 100, 0, 0, 1, [], [new BattlePoisonStatusEffect(3, 10)], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(healerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(healer, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(healer.Id, new BattleAttackUnitTarget(defender.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void CannotCureDeadAllyTest()
    {
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Cure
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, healerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(healerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(healer, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(healer.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetDead);
    }

    [Test]
    public void IdempotentTest()
    {
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Cure
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, healerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = new(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, 80, 0, 0, 1, [], [new BattleCureStatusEffect(), new BattlePoisonStatusEffect(3, 10)], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(healerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(healer, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(healer.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(healer.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("healer")]),
            new BattleUnitAttackedEvent(healer.Id, ally.Id, new UnitAttackDefinitionId("healer")),
            new BattleUnitAttackHitEvent(healer.Id, ally.Id, new UnitAttackDefinitionId("healer"), 0),
            new BattleUnitCuredEvent(ally.Id),
            new BattleUnitTurnEndingEvent(healer.Id),
            new BattleUnitTurnEndedEvent(healer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(healer, GridPosition.FrontBottom, false)
                    .Place(ally with { StatusEffects = [new BattleCureStatusEffect()] }, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void AllReachCureCleanTargetTest()
    {
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("healer", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("healer"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Cure
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, healerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = new(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, 80, 0, 0, 1, [], [new BattlePoisonStatusEffect(3, 10)], null, [], null, false);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(healerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(healer, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(healer.Id, new BattleAttackUnitTarget(healer.Id)), [
            new BattleUnitAttackEvent(healer.Id, new BattleAttackUnitTarget(healer.Id), [new UnitAttackDefinitionId("healer")]),
            new BattleUnitAttackedEvent(healer.Id, ally.Id, new UnitAttackDefinitionId("healer")),
            new BattleUnitAttackHitEvent(healer.Id, ally.Id, new UnitAttackDefinitionId("healer"), 0),
            new BattleUnitCuredEvent(ally.Id),
            new BattleUnitTurnEndingEvent(healer.Id),
            new BattleUnitTurnEndedEvent(healer.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(healer, GridPosition.FrontBottom, false)
                    .Place(ally with { StatusEffects = [new BattleCureStatusEffect()] }, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }
}
