namespace Gallean.Client;

internal readonly record struct UnitGridSlotReference(UnitGridViewModel Grid, GridPosition Position) : IDragSourceReference, IDragTargetReference;
