namespace Gallean.Engine.Contracts;

public sealed record BattleFrostbiteStatusEffect(int RemainingTurns, int Damage) : BattleAbstractDamageOverTimeEffect(RemainingTurns, Damage);
