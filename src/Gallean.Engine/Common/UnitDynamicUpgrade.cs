namespace Gallean.Engine;

public sealed record UnitDynamicUpgrade(
    int MaxHitpoints,
    int Armor,
    int Regeneration,
    int Initiative,
    int Damage,
    int Heal,
    int Accuracy,
    int ExperienceKilled,
    int ExperienceNextLevel,
    int MovementPoints,
    int Negotiate,
    WorldResourceSet PurchaseCost,
    WorldResourceSet ReviveCost,
    WorldResourceSet HealCost,
    WorldResourceSet TrainingCost)
{
    public static UnitDynamicUpgrade Empty { get; } = new(
        MaxHitpoints: 0,
        Armor: 0,
        Regeneration: 0,
        Initiative: 0,
        Damage: 0,
        Heal: 0,
        Accuracy: 0,
        ExperienceKilled: 0,
        ExperienceNextLevel: 0,
        MovementPoints: 0,
        Negotiate: 0,
        WorldResourceSet.Empty,
        WorldResourceSet.Empty,
        WorldResourceSet.Empty,
        WorldResourceSet.Empty);
}
