namespace Gallean.Engine;

public sealed record LeaderUpgradeDefinition(
    LordDefinitionId LordId,
    UnitModifierDefinitionId ModifierId,
    int MinLevel,
    int Priority,
    GlobalTextId NameTextId,
    GlobalTextId DescriptionTextId);
