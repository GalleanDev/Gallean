namespace Gallean.Engine;

public sealed record WorldUnitActionInfo(
    WorldUnitId UnitId,
    WorldCommandError? HealError,
    WorldCommandError? ResurrectError,
    WorldCommandError? DisbandError,
    WorldCommandError? LockTypeError);
