namespace Gallean.Engine.Tests;

public sealed class BattleEngineDifficultyBonusTests
{
    [Test]
    public void AccuracyBonusTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 10, accuracy: 50, reach: UnitAttackReach.Any)
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleModifiers modifiers = new(
            Attacker: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 10, RandomDamageBonus: 0, CanRetreat: true),
            Defender: BattleSideModifiers.None);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithModifiers(modifiers)
            .WithRandom(new TestSequenceRandomProvider(60, 60, 60, 60))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id, 10),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(attacker, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender with { Hitpoints = 10 }, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: modifiers)));
    }

    [Test]
    public void AccuracyPenaltyTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 10, accuracy: 50, reach: UnitAttackReach.Any)
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleModifiers modifiers = new(
            Attacker: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: -15, RandomDamageBonus: 0, CanRetreat: true),
            Defender: BattleSideModifiers.None);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithModifiers(modifiers)
            .WithRandom(new TestSequenceRandomProvider(40, 40, 40, 40))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackMissedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(attacker, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: modifiers)));
    }

    [Test]
    public void RandomDamageBonusTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 20, initiative: 40, damage: 5);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 20);

        BattleModifiers modifiers = new(
            Attacker: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 6, CanRetreat: true),
            Defender: BattleSideModifiers.None);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithModifiers(modifiers)
            .WithGlobals(new GlobalVariables { BattleInitiativeVariance = 0, BattleDamageVariance = 6 })
            .WithRandom(new FixedRandomProvider())
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id, 16),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(attacker, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(defender with { Hitpoints = 4 }, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: modifiers)));
    }
}
