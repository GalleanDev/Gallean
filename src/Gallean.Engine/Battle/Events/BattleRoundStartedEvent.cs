using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record BattleRoundStartedEvent(BattleRound Round, ImmutableArray<BattleUnitTurn> TurnOrder) : BattleEvent
{
    public bool Equals(BattleRoundStartedEvent? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Round == other.Round &&
               TurnOrder.SequenceEqual(other.TurnOrder);
    }

    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Round);
        hash.AddSequence(TurnOrder);

        return hash.ToHashCode();
    }
}
