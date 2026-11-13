namespace Gallean.Engine.Contracts;

public sealed record BattleReduceDamageStatusEffect(int Percent, bool IsInfinite) : BattleAbstractStatModifierEffect(Percent, IsInfinite);
