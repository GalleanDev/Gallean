namespace Gallean.Engine.Tests;

public sealed class BattleTurnOrderCalculatorTests
{
    [Test]
    public void BaseInitiativeTest()
    {
        UnitDefinition fasterDefinition = TestHelpers.SimpleUnitDefinition("faster", maxHitpoints: 10, initiative: 30, damage: 5);
        UnitDefinition slowerDefinition = TestHelpers.SimpleUnitDefinition("slower", maxHitpoints: 10, initiative: 25, damage: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(fasterDefinition)
            .AddUnit(slowerDefinition)
            .Build();

        BattleUnit faster = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, fasterDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit slower = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, slowerDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleState state = new BattleStateBuilder(definitions)
            .Add(faster, GridPosition.FrontBottom)
            .Add(slower, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0 };
        IEngineContext context = new EngineContext(
            new FixedRandomProvider(),
            definitions,
            new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition),
            GameScenarioProvider.Empty,
            globals);

        IReadOnlyList<BattleUnitTurn> turnOrder = BattleTurnOrderCalculator.GetNormalRoundOrder(state, context);

        Assert.That(turnOrder, Is.EqualTo([new BattleUnitTurn(faster.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(slower.Id, BattleUnitTurnType.Normal)]));
    }

    [Test]
    public void RandomInitiativeBonusTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 10, initiative: 30, damage: 5);
        UnitDefinition followerDefinition = TestHelpers.SimpleUnitDefinition("follower", maxHitpoints: 10, initiative: 25, damage: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(followerDefinition)
            .Build();

        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit follower = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, followerDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleState state = new BattleStateBuilder(definitions)
            .Add(leader, GridPosition.FrontBottom)
            .Add(follower, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        GlobalVariables globals = new() { BattleInitiativeVariance = 10 };
        IEngineContext context = new EngineContext(
            new TestSequenceRandomProvider(0, 10),
            definitions,
            new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition),
            GameScenarioProvider.Empty,
            globals);

        IReadOnlyList<BattleUnitTurn> turnOrder = BattleTurnOrderCalculator.GetNormalRoundOrder(state, context);

        Assert.That(turnOrder, Is.EqualTo([new BattleUnitTurn(follower.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(leader.Id, BattleUnitTurnType.Normal)]));
    }

    [Test]
    public void SameInitiativeTest()
    {
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 10, initiative: 50, damage: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(summonerDefinition)
            .AddUnit(defenderDefinition)
            .AddUnit(impDefinition)
            .Build();

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 10);
        BattleUnit imp = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, impDefinition.Id, BattleSide.Attacker, hitpoints: 10) with { SummonerId = summoner.Id };

        BattleState state = new BattleStateBuilder(definitions)
            .Add(summoner, GridPosition.BackBottom)
            .Add(defender, GridPosition.FrontBottom)
            .Add(imp, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0 };
        IEngineContext context = new EngineContext(
            new FixedRandomProvider(),
            definitions,
            new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition),
            GameScenarioProvider.Empty,
            globals);

        IReadOnlyList<BattleUnitTurn> turnOrder = BattleTurnOrderCalculator.GetNormalRoundOrder(state, context);

        Assert.That(turnOrder, Is.EqualTo([new BattleUnitTurn(summoner.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal), new BattleUnitTurn(imp.Id, BattleUnitTurnType.Normal)]));
    }

    [Test]
    public void OpeningRoundTest()
    {
        UnitDefinition slowDefinition = TestHelpers.SimpleUnitDefinition("slow", maxHitpoints: 60, initiative: 40, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("slow"), initiative: 40, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Doppelganger
            }
        };
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 100, initiative: 60, damage: 10);
        UnitDefinition fastDefinition = TestHelpers.SimpleUnitDefinition("fast", maxHitpoints: 60, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("fast"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Doppelganger
            }
        };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(slowDefinition)
            .AddUnit(knightDefinition)
            .AddUnit(fastDefinition)
            .Build();

        BattleUnit slow = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, slowDefinition.Id, BattleSide.Attacker, hitpoints: 60);
        BattleUnit knight = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, knightDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit fast = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, fastDefinition.Id, BattleSide.Defender, hitpoints: 60);

        BattleState state = new BattleStateBuilder(definitions)
            .Add(slow, GridPosition.BackBottom)
            .Add(knight, GridPosition.FrontBottom)
            .Add(fast, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0 };
        IEngineContext context = new EngineContext(
            new FixedRandomProvider(),
            definitions,
            new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition),
            GameScenarioProvider.Empty,
            globals);

        IReadOnlyList<BattleUnitTurn> turnOrder = BattleTurnOrderCalculator.GetOpeningRoundOrder(state, context);

        Assert.That(turnOrder, Is.EqualTo([new BattleUnitTurn(fast.Id, BattleUnitTurnType.Doppelganger), new BattleUnitTurn(slow.Id, BattleUnitTurnType.Doppelganger)]));
    }
}
