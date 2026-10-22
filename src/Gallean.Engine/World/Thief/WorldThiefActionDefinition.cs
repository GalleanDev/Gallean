namespace Gallean.Engine;

public sealed record WorldThiefActionDefinition(
    WorldThiefActionType Type,
    int Chance,
    GlobalTextId NameTextId,
    GlobalTextId DescriptionTextId);
