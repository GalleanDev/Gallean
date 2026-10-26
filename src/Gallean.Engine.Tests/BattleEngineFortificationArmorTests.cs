namespace Gallean.Engine.Tests;

public sealed class BattleEngineFortificationArmorTests
{
    [Test]
    public void SimpleTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 10, initiative: 50, damage: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit defender = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 10, 0, 0, 1, [], [new BattleFortificationStatusEffect(30)], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(defender, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, defender.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 10, Armor: 30, Regeneration: 5, Initiative: 50, Damage: 5, Heal: 0, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void DefendingStackTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 10, initiative: 50, damage: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(definition).Build();
        GlobalVariables globals = new();

        BattleUnit defender = new(new BattleUnitId(0), null, definition.Id, BattleSide.Defender, 10, 0, 0, 1, [], [new BattleDefendStatusEffect(), new BattleFortificationStatusEffect(30)], null, [], null, false);
        BattleState state = new BattleStateBuilder(definitions).Add(defender, GridPosition.FrontBottom).Build(BattleModifiers.None);

        UnitStats stats = new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition).GetBattleUnitStats(state, defender.Id);

        Assert.That(stats, Is.EqualTo(new UnitStats(MaxHitpoints: 10, Armor: 65, Regeneration: 5, Initiative: 50, Damage: 5, Heal: 0, Accuracy: 100, ExperienceKilled: 0, ExperienceNextLevel: 1000)));
    }

    [Test]
    public void LowFortificationDamageTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 50, initiative: 50, damage: 100);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 200, initiative: 40, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = new(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, 200, 0, 0, 1, [], [new BattleFortificationStatusEffect(10)], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
            [
                new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
                new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
                new BattleUnitAttackHitEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id, 90),
                new BattleUnitTurnEndingEvent(attacker.Id),
                new BattleUnitTurnEndedEvent(attacker.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, new GridPosition(0, 0), false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 110 }, new GridPosition(0, 0), false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }

    [Test]
    public void HighFortificationDamageTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 50, initiative: 50, damage: 100);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 200, initiative: 40, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit defender = new(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, 200, 0, 0, 1, [], [new BattleFortificationStatusEffect(30)], null, [], null, false);

        BattleEngine engine = new TestBattleEngineBuilder()
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithAttacker(attacker, new GridPosition(0, 0))
            .WithDefender(defender, new GridPosition(0, 0))
            .Build();

        engine.CheckCommand(
            new BattleAttackCommand(attacker.Id, new BattleAttackUnitTarget(defender.Id)),
            [
                new BattleUnitAttackEvent(attacker.Id, new BattleAttackUnitTarget(defender.Id), [attackerDefinition.Attack.Id]),
                new BattleUnitAttackedEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id),
                new BattleUnitAttackHitEvent(attacker.Id, defender.Id, attackerDefinition.Attack.Id, 70),
                new BattleUnitTurnEndingEvent(attacker.Id),
                new BattleUnitTurnEndedEvent(attacker.Id),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                AttackerGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(attacker, new GridPosition(0, 0), false)
                    .Build(),
                DefenderGrid = new UnitGrid<BattleUnit>.Builder()
                    .Place(defender with { Hitpoints = 130 }, new GridPosition(0, 0), false)
                    .Build(),
                CurrentUnitTurn = new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal),
                CurrentRound = BattleRound.Normal(1)
            }));
    }
}
