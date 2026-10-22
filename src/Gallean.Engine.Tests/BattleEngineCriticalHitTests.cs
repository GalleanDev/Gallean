namespace Gallean.Engine.Tests;

public sealed class BattleEngineCriticalHitTests
{
    [Test]
    public void AttackModifierTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 100) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 100, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                HasCriticalHit = true
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 200, initiative: 40, damage: 5) with
        {
            Armor = 50
        };
        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 99, 99, 1, 1, 99, 99))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id, 55),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 145 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void LeaderAbilityTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 100) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.CriticalHit
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 200, initiative: 40, damage: 5) with
        {
            Armor = 90
        };
        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 99, 99, 1, 1, 99, 99))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id, 15),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 185 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void SeparateRollTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 100) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 100, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                HasCriticalHit = true
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 200, initiative: 40, damage: 5) with
        {
            Armor = 50
        };
        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithGlobals(new GlobalVariables
            {
                BattleInitiativeVariance = 0,
                BattleDamageVariance = 0,
                CriticalHitAccuracy = 50
            })
            .WithRandom(new TestSequenceRandomProvider(1, 1, 99, 99, 1, 1, 99, 99))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id, 50),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 150 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }

    [Test]
    public void DrainTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 100) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 100, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.DrainLife,
                HasCriticalHit = true
            }
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 200, initiative: 40, damage: 5) with
        {
            Armor = 50
        };
        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, GridPosition.FrontBottom)
            .WithDefender(defender, GridPosition.FrontBottom)
            .WithRandom(new TestSequenceRandomProvider(1, 1, 99, 99, 1, 1, 99, 99))
            .Build();

        engine.CheckCommand(new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)), [
            new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
            new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
            new BattleUnitAttackHitEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id, 50),
            new BattleUnitHealedEvent(attacker.Id, 75),
            new BattleUnitTurnEndingEvent(attacker.Id),
            new BattleUnitTurnEndedEvent(attacker.Id),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
        ]);

        Assert.That(engine.State, Is.EqualTo(new BattleState(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker with { Hitpoints = 75 }, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender with { Hitpoints = 150 }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None)));
    }
}
