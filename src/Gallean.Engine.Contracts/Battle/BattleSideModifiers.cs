namespace Gallean.Engine.Contracts;

public sealed record BattleSideModifiers(int ExperiencePercent, int AccuracyBonus, int RandomDamageBonus, bool CanRetreat)
{
    public static BattleSideModifiers None { get; } = new(0, 0, 0, CanRetreat: true);
}
