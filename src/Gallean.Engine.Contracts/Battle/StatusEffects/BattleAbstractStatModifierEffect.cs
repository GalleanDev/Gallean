namespace Gallean.Engine.Contracts;

public abstract record BattleAbstractStatModifierEffect(int Percent, bool IsInfinite) : BattleAbstractStatusEffect;
