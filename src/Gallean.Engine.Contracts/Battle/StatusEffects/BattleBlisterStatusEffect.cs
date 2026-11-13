namespace Gallean.Engine.Contracts;

public sealed record BattleBlisterStatusEffect(int RemainingTurns, int Damage) : BattleAbstractDamageOverTimeEffect(RemainingTurns, Damage);
