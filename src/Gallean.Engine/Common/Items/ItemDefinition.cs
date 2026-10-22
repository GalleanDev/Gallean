namespace Gallean.Engine;

public sealed record ItemDefinition(
    ItemDefinitionId Id,
    GlobalTextId NameTextId,
    GlobalTextId DescriptionTextId,
    ItemCategory Category,
    WorldResourceSet Value,
    UnitModifierDefinitionId? EquipmentModifierId,
    UnitAttackDefinitionId? EquipmentAttackId,
    ItemEffect? Effect,
    UnitDefinitionId? AnimationUnitId);
