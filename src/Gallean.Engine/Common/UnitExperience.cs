namespace Gallean.Engine;

public sealed record UnitExperience(
    int Current,
    int NextLevel,
    bool IsMaximumLevel,
    bool IsPendingUpgrade);
