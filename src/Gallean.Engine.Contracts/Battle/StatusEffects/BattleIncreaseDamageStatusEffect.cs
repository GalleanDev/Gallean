namespace Gallean.Engine.Contracts;

public sealed record BattleIncreaseDamageStatusEffect(int Percent, bool IsInfinite) : BattleAbstractStatModifierEffect(Percent, IsInfinite);
