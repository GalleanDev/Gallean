using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record BattleUnitAttackStepEvent(
    BattleUnitId AttackerId,
    ImmutableArray<UnitAttackDefinitionId> AttackIds,
    ImmutableArray<BattleUnitId> TargetIds,
    int TargetIndex,
    ImmutableArray<BattleUnitId> NextTargetIds,
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
               AttackIds.SequenceEqual(other.AttackIds) &&
               TargetIds.SequenceEqual(other.TargetIds) &&
               TargetIndex == other.TargetIndex &&
               NextTargetIds.SequenceEqual(other.NextTargetIds) &&
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
        hash.AddSequence(AttackIds);
        hash.AddSequence(TargetIds);
        hash.Add(TargetIndex);
        hash.AddSequence(NextTargetIds);
        hash.Add(AccuracyReduction);

        return hash.ToHashCode();
    }
}
