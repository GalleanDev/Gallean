namespace Gallean.Engine;

public sealed record UnitExperience(
    int Current,
    int NextLevel,
    bool CanGainExperience,
    bool IsMaximumLevel,
    bool IsPendingUpgrade);
