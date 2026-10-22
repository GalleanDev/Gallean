using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record BattleUnitAttackStepEvent(
    BattleUnitId AttackerId,
    BattleUnitId TargetId,
    ImmutableArray<UnitAttackDefinitionId> AttackIds,
    int AccuracyReduction) : BattleEvent
{
    public bool Equals(BattleUnitAttackStepEvent? other)
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
               TargetId == other.TargetId &&
               AttackIds.SequenceEqual(other.AttackIds) &&
               AccuracyReduction == other.AccuracyReduction;
    }

    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(AttackerId);
        hash.Add(TargetId);
        hash.AddSequence(AttackIds);
        hash.Add(AccuracyReduction);

        return hash.ToHashCode();
    }
}
