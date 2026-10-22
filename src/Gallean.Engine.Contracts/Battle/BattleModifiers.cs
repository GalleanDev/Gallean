namespace Gallean.Engine.Contracts;

public sealed record BattleModifiers(BattleSideModifiers Attacker, BattleSideModifiers Defender)
{
    public static BattleModifiers None { get; } = new(BattleSideModifiers.None, BattleSideModifiers.None);
}
