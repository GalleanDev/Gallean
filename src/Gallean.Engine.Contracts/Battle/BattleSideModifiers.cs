namespace Gallean.Engine.Contracts;

public sealed record BattleSideModifiers(int ExperiencePercent, int AccuracyBonus, int RandomDamageBonus)
{
    public static BattleSideModifiers None { get; } = new(0, 0, 0);
}
