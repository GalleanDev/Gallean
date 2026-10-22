namespace Gallean.Engine.Contracts;

public sealed record WorldMercenaryUnit(
    UnitDefinitionId DefinitionId,
    int Level,
    bool IsUnique);
