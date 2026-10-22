namespace Gallean.Engine.Contracts;

public abstract record BattleAbstractDamageOverTimeEffect(int RemainingTurns, int Damage) : BattleAbstractStatusEffect;
