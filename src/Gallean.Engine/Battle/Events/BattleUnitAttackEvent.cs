using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record BattleUnitAttackEvent(
    BattleUnitId AttackerId,
    BattleAttackTarget Target,
    ImmutableArray<UnitAttackDefinitionId> AttackIds) : BattleEvent
{
    public bool Equals(BattleUnitAttackEvent? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return AttackerId == other.AttackerId &&
               Target == other.Target &&
               AttackIds.SequenceEqual(other.AttackIds);
    }

    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(AttackerId);
        hash.Add(Target);
        hash.AddSequence(AttackIds);

        return hash.ToHashCode();
    }
}
