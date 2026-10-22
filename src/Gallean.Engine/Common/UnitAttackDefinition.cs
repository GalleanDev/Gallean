using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record UnitAttackDefinition(
    UnitAttackDefinitionId Id,
    UnitAttackType Type,
    UnitAttackSource Source,
    int Initiative,
    int Accuracy,
    UnitAttackReach Reach,
    int Damage,
    int Heal,
    int Level,
    bool IsInfinite,
    ImmutableArray<UnitDefinitionId> SummonUnitIds,
    UnitDefinitionId? SmallTransformUnitId,
    UnitDefinitionId? LargeTransformUnitId,
    bool HasCriticalHit,
    UnitAttackDefinitionId? AlternativeAttackId,
    ImmutableArray<UnitModifierDefinitionId> WardModifierIds)
{
    public bool Equals(UnitAttackDefinition? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id &&
               Type == other.Type &&
               Source == other.Source &&
               Initiative == other.Initiative &&
               Accuracy == other.Accuracy &&
               Reach == other.Reach &&
               Damage == other.Damage &&
               Heal == other.Heal &&
               Level == other.Level &&
               IsInfinite == other.IsInfinite &&
               SummonUnitIds.SequenceEqual(other.SummonUnitIds) &&
               SmallTransformUnitId == other.SmallTransformUnitId &&
               LargeTransformUnitId == other.LargeTransformUnitId &&
               HasCriticalHit == other.HasCriticalHit &&
               AlternativeAttackId == other.AlternativeAttackId &&
               WardModifierIds.SequenceEqual(other.WardModifierIds);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Id);
        hash.Add(Type);
        hash.Add(Source);
        hash.Add(Initiative);
        hash.Add(Accuracy);
        hash.Add(Reach);
        hash.Add(Damage);
        hash.Add(Heal);
        hash.Add(Level);
        hash.Add(IsInfinite);
        hash.AddSequence(SummonUnitIds);
        hash.Add(SmallTransformUnitId);
        hash.Add(LargeTransformUnitId);
        hash.Add(HasCriticalHit);
        hash.Add(AlternativeAttackId);
        hash.AddSequence(WardModifierIds);

        return hash.ToHashCode();
    }
}
