namespace Gallean.Engine;

public sealed record UnitStats(
    int MaxHitpoints,
    int Armor,
    int Regeneration,
    int Initiative,
    int Damage,
    int Accuracy,
    int ExperienceKilled,
    int ExperienceNextLevel);
