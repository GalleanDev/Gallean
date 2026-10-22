namespace Gallean.Engine.Contracts;

public sealed record WorldItem(
    WorldItemId Id,
    ItemDefinitionId DefinitionId,
    int? Charges);
