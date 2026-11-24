namespace Gallean.Engine;

public sealed record UnitAttackStats(
    int Accuracy,
    int AccuracyReductionPerTarget,
    int Damage,
    int Heal,
    int DrainPercent,
    int IncreaseDamagePercent);
