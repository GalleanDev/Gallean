namespace Gallean.Engine.Tests;

public sealed class BattleStateTests
{
    [Test]
    public void EqualityTest()
    {
        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, new UnitDefinitionId("attacker"), BattleSide.Attacker, hitpoints: 100);
        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, new UnitDefinitionId("defender"), BattleSide.Defender, hitpoints: 100);
        BattleState state = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None
        );
        BattleState equalState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(attacker, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(defender, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(attacker.Id, BattleUnitTurnType.Normal),
            TurnQueue: [new BattleUnitTurn(defender.Id, BattleUnitTurnType.Normal)],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None
        );

        Assert.That(equalState.AttackerGrid, Is.Not.SameAs(state.AttackerGrid));
        Assert.That(equalState.DefenderGrid, Is.Not.SameAs(state.DefenderGrid));
        Assert.That(equalState, Is.EqualTo(state));
        Assert.That(equalState.GetHashCode(), Is.EqualTo(state.GetHashCode()));

        BattleState differentState = equalState with { CurrentRound = BattleRound.Normal(2) };
        Assert.That(differentState, Is.Not.EqualTo(state));
    }
}
