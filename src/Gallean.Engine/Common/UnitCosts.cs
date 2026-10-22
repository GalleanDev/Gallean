namespace Gallean.Engine;

public sealed record UnitCosts(
    WorldResourceSet PurchaseCost,
    WorldResourceSet ReviveCost,
    WorldResourceSet HealCost,
    WorldResourceSet TrainingCost);
