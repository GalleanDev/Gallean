using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record BattleUnit(
    BattleUnitId Id,
    WorldUnitId? WorldUnitId,
    UnitDefinitionId DefinitionId,
    BattleSide Side,
    int Hitpoints,
    int Experience,
    int ExperienceGained,
    int Level,
    ImmutableArray<UnitModifier> Modifiers,
    ImmutableArray<BattleAbstractStatusEffect> StatusEffects,
    BattleUnitId? SummonerId,
    ImmutableArray<UnitAttackDefinitionId> EquipmentAttackIds,
    string? Name,
    bool IsTypeLocked)
{
    public ImmutableArray<UnitWard> Wards { get; init; } = [];
    public ImmutableArray<BattleUnitWardModifier> WardModifiers { get; init; } = [];
    public bool IsRevived { get; init; }
    public bool IsAlive => Hitpoints > 0;

    public bool Equals(BattleUnit? other)
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
               WorldUnitId == other.WorldUnitId &&
               DefinitionId == other.DefinitionId &&
               Side == other.Side &&
               Hitpoints == other.Hitpoints &&
               Experience == other.Experience &&
               ExperienceGained == other.ExperienceGained &&
               Level == other.Level &&
               Modifiers.SequenceEqual(other.Modifiers) &&
               StatusEffects.SequenceEqual(other.StatusEffects) &&
               SummonerId == other.SummonerId &&
               EquipmentAttackIds.SequenceEqual(other.EquipmentAttackIds) &&
               Name == other.Name &&
               Wards.SequenceEqual(other.Wards) &&
               WardModifiers.SequenceEqual(other.WardModifiers) &&
               IsRevived == other.IsRevived &&
               IsTypeLocked == other.IsTypeLocked;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Id);
        hash.Add(WorldUnitId);
        hash.Add(DefinitionId);
        hash.Add(Side);
        hash.Add(Hitpoints);
        hash.Add(Experience);
        hash.Add(ExperienceGained);
        hash.Add(Level);
        hash.AddSequence(Modifiers);
        hash.AddSequence(StatusEffects);
        hash.Add(SummonerId);
        hash.AddSequence(EquipmentAttackIds);
        hash.Add(Name);
        hash.AddSequence(Wards);
        hash.AddSequence(WardModifiers);
        hash.Add(IsRevived);
        hash.Add(IsTypeLocked);

        return hash.ToHashCode();
    }
}
