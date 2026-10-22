using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record BattleTransformStatusEffect(
    BattleTransformType Type,
    UnitDefinitionId RestoreDefinitionId,
    int RestoreLevel,
    ImmutableArray<UnitWard> RestoreWards,
    int? RemainingTurns) : BattleAbstractStatusEffect
{
    public override TResult Accept<TContext, TResult>(IBattleStatusEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public bool Equals(BattleTransformStatusEffect? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Type == other.Type &&
               RestoreDefinitionId == other.RestoreDefinitionId &&
               RestoreLevel == other.RestoreLevel &&
               RestoreWards.SequenceEqual(other.RestoreWards) &&
               RemainingTurns == other.RemainingTurns;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Type);
        hash.Add(RestoreDefinitionId);
        hash.Add(RestoreLevel);
        hash.AddSequence(RestoreWards);
        hash.Add(RemainingTurns);

        return hash.ToHashCode();
    }
}
