namespace Gallean.Engine.Contracts;

public closed record BattleAbstractDamageOverTimeEffect(int RemainingTurns, int Damage) : BattleAbstractStatusEffect;
