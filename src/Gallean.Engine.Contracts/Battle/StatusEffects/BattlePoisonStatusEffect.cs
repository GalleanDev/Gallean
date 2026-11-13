namespace Gallean.Engine.Contracts;

public sealed record BattlePoisonStatusEffect(int RemainingTurns, int Damage) : BattleAbstractDamageOverTimeEffect(RemainingTurns, Damage);
