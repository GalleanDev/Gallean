namespace Gallean.Engine.Tests;

public sealed class BattleEngineReviveAttackTests
{
    [Test]
    public void RevivesDeadAllyTest()
    {
        UnitDefinition priestDefinition = TestHelpers.SimpleUnitDefinition("priest", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("priest"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Revive,
                Heal = 40
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit priest = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, priestDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(priestDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(priest, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(priest.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(priest.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("priest")]),
            new BattleUnitAttackedEvent(priest.Id, ally.Id, new UnitAttackDefinitionId("priest")),
            new BattleUnitAttackHitEvent(priest.Id, ally.Id, new UnitAttackDefinitionId("priest"), 0),
            new BattleUnitRevivedEvent(ally.Id, 50),
            new BattleUnitTurnEndingEvent(priest.Id),
            new BattleUnitTurnEndedEvent(priest.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(priest, GridPosition.FrontBottom, false)
                    .Place(ally with { Hitpoints = 50, IsRevived = true }, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void AllReachReviveTest()
    {
        UnitDefinition priestDefinition = TestHelpers.SimpleUnitDefinition("priest", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("priest"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Revive,
                Heal = 40
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit priest = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, priestDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit allyA = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit allyB = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(priestDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(priest, GridPosition.FrontBottom)
            .WithAttacker(allyA, GridPosition.FrontCenter)
            .WithAttacker(allyB, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(priest.Id, new BattleAttackUnitTarget(allyA.Id)), [
            new BattleUnitAttackEvent(priest.Id, new BattleAttackUnitTarget(allyA.Id), [new UnitAttackDefinitionId("priest")]),
            new BattleUnitAttackedEvent(priest.Id, allyA.Id, new UnitAttackDefinitionId("priest")),
            new BattleUnitAttackHitEvent(priest.Id, allyA.Id, new UnitAttackDefinitionId("priest"), 0),
            new BattleUnitRevivedEvent(allyA.Id, 50),
            new BattleUnitAttackedEvent(priest.Id, allyB.Id, new UnitAttackDefinitionId("priest")),
            new BattleUnitAttackHitEvent(priest.Id, allyB.Id, new UnitAttackDefinitionId("priest"), 0),
            new BattleUnitRevivedEvent(allyB.Id, 50),
            new BattleUnitTurnEndingEvent(priest.Id),
            new BattleUnitTurnEndedEvent(priest.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(priest, GridPosition.FrontBottom, false)
                    .Place(allyA with { Hitpoints = 50, IsRevived = true }, GridPosition.FrontCenter, false)
                    .Place(allyB with { Hitpoints = 50, IsRevived = true }, GridPosition.BackBottom, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void CannotReviveLivingAllyTest()
    {
        UnitDefinition priestDefinition = TestHelpers.SimpleUnitDefinition("priest", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("priest"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Revive,
                Heal = 40
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit priest = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, priestDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(priestDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(priest, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(priest.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetNotDead);
    }

    [Test]
    public void CannotReviveEnemyTest()
    {
        UnitDefinition priestDefinition = TestHelpers.SimpleUnitDefinition("priest", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("priest"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Revive,
                Heal = 40
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit priest = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, priestDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(priestDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(priest, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(priest.Id, new BattleAttackUnitTarget(defender.Id)),
            BattleCommandError.TargetIncorrect);
    }

    [Test]
    public void AlreadyRevivedTest()
    {
        UnitDefinition priestDefinition = TestHelpers.SimpleUnitDefinition("priest", maxHitpoints: 100, initiative: 60, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("priest"), initiative: 60, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Revive,
                Heal = 40
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5);

        BattleUnit priest = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, priestDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0) with { IsRevived = true };
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(priestDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(priest, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.BackBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommandError(
            new BattleAttackCommand(priest.Id, new BattleAttackUnitTarget(ally.Id)),
            BattleCommandError.TargetAlreadyRevived);
    }

    [Test]
    public void RevivedUnitNoEffectsTest()
    {
        UnitDefinition priestDefinition = TestHelpers.SimpleUnitDefinition("priest", maxHitpoints: 100, initiative: 40, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("priest"), initiative: 40, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Revive,
                Heal = 40
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 60, damage: 10);

        BattleUnit priest = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, priestDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 10) with
        {
            StatusEffects = [new BattleParalyzeStatusEffect(2), new BattlePoisonStatusEffect(2, 5), new BattleShatterArmorStatusEffect(20)]
        };
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(priestDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(priest, GridPosition.FrontBottom)
            .WithAttacker(ally, GridPosition.FrontCenter)
            .WithDefender(defender, GridPosition.FrontBottom)
            .Build();

        engine.CheckCommand(new BattleAttackCommand(defender.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(defender.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("defender")]),
            new BattleUnitAttackedEvent(defender.Id, ally.Id, new UnitAttackDefinitionId("defender")),
            new BattleUnitAttackHitEvent(defender.Id, ally.Id, new UnitAttackDefinitionId("defender"), 10),
            new BattleUnitDiedEvent(ally.Id),
            new BattleUnitTurnEndingEvent(defender.Id),
            new BattleUnitTurnEndedEvent(defender.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(priest.Id, BattleUnitTurnType.Normal))
        ]);

        engine.CheckCommand(new BattleAttackCommand(priest.Id, new BattleAttackUnitTarget(ally.Id)), [
            new BattleUnitAttackEvent(priest.Id, new BattleAttackUnitTarget(ally.Id), [new UnitAttackDefinitionId("priest")]),
            new BattleUnitAttackedEvent(priest.Id, ally.Id, new UnitAttackDefinitionId("priest")),
            new BattleUnitAttackHitEvent(priest.Id, ally.Id, new UnitAttackDefinitionId("priest"), 0),
            new BattleUnitRevivedEvent(ally.Id, 50),
            new BattleUnitTurnEndingEvent(priest.Id),
            new BattleUnitTurnEndedEvent(priest.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(priest, GridPosition.FrontBottom, false)
                    .Place(ally with { Hitpoints = 50, IsRevived = true, StatusEffects = [new BattleShatterArmorStatusEffect(20)] }, GridPosition.FrontCenter, false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender, GridPosition.FrontBottom, false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(ally.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }
}
