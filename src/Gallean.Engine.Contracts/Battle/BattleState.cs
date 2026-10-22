using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record BattleState(
    UnitGrid<BattleUnit> AttackerGrid,
    UnitGrid<BattleUnit> DefenderGrid,
    BattleUnitTurn CurrentUnitTurn,
    ImmutableArray<BattleUnitTurn> TurnQueue,
    ImmutableArray<BattleUnitTurn> WaitingQueue,
    ImmutableArray<BattleItem> Items,
    BattleRound CurrentRound,
    BattleOutcome? Outcome,
    BattleModifiers Modifiers)
{
    public BattleUnitId CurrentUnitId => CurrentUnitTurn.UnitId;
    public int RoundNumber => CurrentRound.Number;

    public bool Equals(BattleState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return AttackerGrid == other.AttackerGrid &&
               DefenderGrid == other.DefenderGrid &&
               CurrentUnitTurn == other.CurrentUnitTurn &&
               TurnQueue.SequenceEqual(other.TurnQueue) &&
               WaitingQueue.SequenceEqual(other.WaitingQueue) &&
               Items.SequenceEqual(other.Items) &&
               CurrentRound == other.CurrentRound &&
               Outcome == other.Outcome &&
               Modifiers == other.Modifiers;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(AttackerGrid);
        hash.Add(DefenderGrid);
        hash.Add(CurrentUnitTurn);
        hash.AddSequence(TurnQueue);
        hash.AddSequence(WaitingQueue);
        hash.AddSequence(Items);
        hash.Add(CurrentRound);
        hash.Add(Outcome);
        hash.Add(Modifiers);

        return hash.ToHashCode();
    }
}
