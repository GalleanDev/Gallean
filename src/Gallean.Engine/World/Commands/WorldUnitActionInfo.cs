namespace Gallean.Engine;

public sealed record WorldUnitActionInfo(
    WorldUnitId UnitId,
    WorldResourceSet HealCost,
    WorldCommandError? HealError,
    WorldResourceSet ResurrectCost,
    WorldCommandError? ResurrectError,
    WorldCommandError? DisbandError,
    WorldCommandError? LockTypeError);
