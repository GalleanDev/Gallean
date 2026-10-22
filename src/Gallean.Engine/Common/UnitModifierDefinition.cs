using System.Collections.Immutable;

namespace Gallean.Engine;

public sealed record UnitModifierDefinition(
    UnitModifierDefinitionId Id,
    GlobalTextId DescriptionTextId,
    UnitMoveAbility MoveAbility,
    UnitStatModifier MaxMovement,
    UnitStatModifier Leadership,
    UnitStatModifier VisionRadius,
    UnitStatModifier Hitpoints,
    int RegenerationPercent,
    UnitStatModifier Armor,
    UnitStatModifier Initiative,
    UnitStatModifier Damage,
    UnitStatModifier Accuracy,
    int DrainPercent,
    bool FastRetreat,
    int PurchaseDiscountPercent,
    LeaderAbility LeaderAbilities,
    ImmutableArray<UnitWard> Wards);
