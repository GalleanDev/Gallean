namespace Gallean.Client;

internal readonly record struct MercenaryReference(UnitDefinitionId UnitDefinitionId, int Level) : IDragSourceReference;
