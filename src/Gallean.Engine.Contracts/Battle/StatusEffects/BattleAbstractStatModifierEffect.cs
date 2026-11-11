namespace Gallean.Engine.Contracts;

public closed record BattleAbstractStatModifierEffect(int Percent, bool IsInfinite) : BattleAbstractStatusEffect;
