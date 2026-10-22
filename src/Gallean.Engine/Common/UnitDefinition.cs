using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record UnitDefinition(
    UnitDefinitionId Id,
    GlobalTextId NameTextId,
    GlobalTextId DescriptionTextId,
    GlobalTextId AbilityTextId,
    int MaxHitpoints,
    int Armor,
    int Regeneration,
    UnitAttackDefinition Attack,
    UnitAttackDefinition? SecondaryAttack,
    bool AttacksTwice,
    UnitCategory Category,
    UnitLeaderCategory? LeaderCategory,
    WorldRace Race,
    WorldSubraceType Subrace,
    bool IsMale,
    UnitDeathAnimation DeathAnimation,
    bool IsLarge,
    bool IsWaterOnly,
    UnitMoveAbility MoveAbility,
    int? VisionRadius,
    WorldResourceSet PurchaseCost,
    WorldResourceSet ReviveCost,
    WorldResourceSet HealCost,
    WorldResourceSet TrainingCost,
    WorldPlayerBuildingDefinitionId? RequiredBuildingId,
    int ExperienceKilled,
    int ExperienceNextLevel,
    int Level,
    int? Leadership,
    int? Negotiate,
    int? MovementPoints,
    int? Lifetime,
    LeaderAbility LeaderAbilities,
    UnitDefinitionId? PreviousUnitId,
    ImmutableArray<UnitWard> Wards)
{
    public bool Equals(UnitDefinition? other)
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
               NameTextId == other.NameTextId &&
               DescriptionTextId == other.DescriptionTextId &&
               AbilityTextId == other.AbilityTextId &&
               MaxHitpoints == other.MaxHitpoints &&
               Armor == other.Armor &&
               Regeneration == other.Regeneration &&
               Attack == other.Attack &&
               SecondaryAttack == other.SecondaryAttack &&
               AttacksTwice == other.AttacksTwice &&
               Category == other.Category &&
               LeaderCategory == other.LeaderCategory &&
               Race == other.Race &&
               Subrace == other.Subrace &&
               IsMale == other.IsMale &&
               DeathAnimation == other.DeathAnimation &&
               IsLarge == other.IsLarge &&
               IsWaterOnly == other.IsWaterOnly &&
               MoveAbility == other.MoveAbility &&
               VisionRadius == other.VisionRadius &&
               PurchaseCost == other.PurchaseCost &&
               ReviveCost == other.ReviveCost &&
               HealCost == other.HealCost &&
               TrainingCost == other.TrainingCost &&
               RequiredBuildingId == other.RequiredBuildingId &&
               ExperienceKilled == other.ExperienceKilled &&
               ExperienceNextLevel == other.ExperienceNextLevel &&
               Level == other.Level &&
               Leadership == other.Leadership &&
               Negotiate == other.Negotiate &&
               MovementPoints == other.MovementPoints &&
               Lifetime == other.Lifetime &&
               LeaderAbilities == other.LeaderAbilities &&
               PreviousUnitId == other.PreviousUnitId &&
               Wards.SequenceEqual(other.Wards);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Id);
        hash.Add(NameTextId);
        hash.Add(DescriptionTextId);
        hash.Add(AbilityTextId);
        hash.Add(MaxHitpoints);
        hash.Add(Armor);
        hash.Add(Regeneration);
        hash.Add(Attack);
        hash.Add(SecondaryAttack);
        hash.Add(AttacksTwice);
        hash.Add(Category);
        hash.Add(LeaderCategory);
        hash.Add(Race);
        hash.Add(Subrace);
        hash.Add(IsMale);
        hash.Add(DeathAnimation);
        hash.Add(IsLarge);
        hash.Add(IsWaterOnly);
        hash.Add(MoveAbility);
        hash.Add(VisionRadius);
        hash.Add(PurchaseCost);
        hash.Add(ReviveCost);
        hash.Add(HealCost);
        hash.Add(TrainingCost);
        hash.Add(RequiredBuildingId);
        hash.Add(ExperienceKilled);
        hash.Add(ExperienceNextLevel);
        hash.Add(Level);
        hash.Add(Leadership);
        hash.Add(Negotiate);
        hash.Add(MovementPoints);
        hash.Add(Lifetime);
        hash.Add(LeaderAbilities);
        hash.Add(PreviousUnitId);
        hash.AddSequence(Wards);

        return hash.ToHashCode();
    }
}
