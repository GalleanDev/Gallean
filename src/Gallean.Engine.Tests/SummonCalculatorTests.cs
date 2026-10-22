namespace Gallean.Engine.Tests;

public sealed class SummonCalculatorTests
{
    private static UnitAttackDefinition SummonAttack(params UnitDefinitionId[] summonUnitIds)
    {
        return TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summon"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Type = UnitAttackType.Summon,
            SummonUnitIds = [.. summonUnitIds]
        };
    }

    [Test]
    public void GetSummonPlacementsRandomCandidateTest()
    {
        UnitDefinition meleeDefinition = TestHelpers.SimpleUnitDefinition("melee", maxHitpoints: 20, initiative: 30, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("melee"), initiative: 30, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };
        UnitDefinition rangedDefinition = TestHelpers.SimpleUnitDefinition("ranged", maxHitpoints: 15, initiative: 40, damage: 4);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(meleeDefinition)
            .AddUnit(rangedDefinition)
            .Build();

        BattleState state = new(
            AttackerGrid: UnitGrid<BattleUnit>.Empty,
            DefenderGrid: UnitGrid<BattleUnit>.Empty,
            CurrentUnitTurn: default,
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IReadOnlyList<BattleSummonPlacement> actual = SummonCalculator.GetSummonPlacements(
            SummonAttack(meleeDefinition.Id, rangedDefinition.Id),
            state,
            BattleSide.Attacker,
            [GridPosition.FrontBottom],
            definitions,
            new TestSequenceRandomProvider(1));

        Assert.That(actual, Is.EqualTo([new BattleSummonPlacement(rangedDefinition.Id, GridPosition.FrontBottom)]));
    }

    [Test]
    public void GetSummonPlacementsBackLineTest()
    {
        UnitDefinition meleeDefinition = TestHelpers.SimpleUnitDefinition("melee", maxHitpoints: 20, initiative: 30, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("melee"), initiative: 30, damage: 5, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };
        UnitDefinition rangedDefinition = TestHelpers.SimpleUnitDefinition("ranged", maxHitpoints: 15, initiative: 40, damage: 4);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(meleeDefinition)
            .AddUnit(rangedDefinition)
            .Build();

        BattleState state = new(
            AttackerGrid: UnitGrid<BattleUnit>.Empty,
            DefenderGrid: UnitGrid<BattleUnit>.Empty,
            CurrentUnitTurn: default,
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IReadOnlyList<BattleSummonPlacement> actual = SummonCalculator.GetSummonPlacements(
            SummonAttack(meleeDefinition.Id, rangedDefinition.Id),
            state,
            BattleSide.Attacker,
            [GridPosition.BackBottom],
            definitions,
            new TestSequenceRandomProvider(0));

        Assert.That(actual, Is.EqualTo([new BattleSummonPlacement(rangedDefinition.Id, GridPosition.BackBottom)]));
    }

    [Test]
    public void GetSummonPlacementsLargeUnitSpaceTest()
    {
        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 40, initiative: 30, damage: 8) with
        {
            IsLarge = true
        };
        UnitDefinition smallDefinition = TestHelpers.SimpleUnitDefinition("small", maxHitpoints: 15, initiative: 40, damage: 4);
        UnitDefinition blockerDefinition = TestHelpers.SimpleUnitDefinition("blocker", maxHitpoints: 10, initiative: 10, damage: 1);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(largeDefinition)
            .AddUnit(smallDefinition)
            .AddUnit(blockerDefinition)
            .Build();
        BattleUnit blocker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, blockerDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        UnitGrid<BattleUnit> grid = new UnitGrid<BattleUnit>.Builder()
            .Place(blocker, GridPosition.BackBottom, false)
            .Build();

        BattleState state = new(
            AttackerGrid: grid,
            DefenderGrid: UnitGrid<BattleUnit>.Empty,
            CurrentUnitTurn: default,
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IReadOnlyList<BattleSummonPlacement> actual = SummonCalculator.GetSummonPlacements(
            SummonAttack(largeDefinition.Id, smallDefinition.Id),
            state,
            BattleSide.Attacker,
            [GridPosition.FrontBottom],
            definitions,
            new TestSequenceRandomProvider(0));

        Assert.That(actual, Is.EqualTo([new BattleSummonPlacement(smallDefinition.Id, GridPosition.FrontBottom)]));
    }

    [Test]
    public void GetSummonPlacementsLargeUnitTest()
    {
        UnitDefinition smallDefinition = TestHelpers.SimpleUnitDefinition("small", maxHitpoints: 15, initiative: 40, damage: 4);
        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 40, initiative: 30, damage: 8) with
        {
            IsLarge = true
        };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(smallDefinition)
            .AddUnit(largeDefinition)
            .Build();

        BattleState state = new(
            AttackerGrid: UnitGrid<BattleUnit>.Empty,
            DefenderGrid: UnitGrid<BattleUnit>.Empty,
            CurrentUnitTurn: default,
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IReadOnlyList<BattleSummonPlacement> actual = SummonCalculator.GetSummonPlacements(
            SummonAttack(smallDefinition.Id, largeDefinition.Id),
            state,
            BattleSide.Attacker,
            [GridPosition.FrontBottom],
            definitions,
            new TestSequenceRandomProvider(0));

        Assert.That(actual, Is.EqualTo([new BattleSummonPlacement(largeDefinition.Id, GridPosition.FrontBottom)]));
    }

    [Test]
    public void GetSummonPlacementsLargeBackLineTest()
    {
        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 40, initiative: 30, damage: 8) with
        {
            IsLarge = true
        };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(largeDefinition)
            .Build();

        BattleState state = new(
            AttackerGrid: UnitGrid<BattleUnit>.Empty,
            DefenderGrid: UnitGrid<BattleUnit>.Empty,
            CurrentUnitTurn: default,
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IReadOnlyList<BattleSummonPlacement> actual = SummonCalculator.GetSummonPlacements(
            SummonAttack(largeDefinition.Id),
            state,
            BattleSide.Attacker,
            [GridPosition.FrontBottom, GridPosition.BackBottom, GridPosition.FrontCenter],
            definitions,
            new TestSequenceRandomProvider(0, 0));

        Assert.That(actual, Is.EqualTo([
            new BattleSummonPlacement(largeDefinition.Id, GridPosition.FrontBottom),
            new BattleSummonPlacement(largeDefinition.Id, GridPosition.FrontCenter)
        ]));
    }
}
