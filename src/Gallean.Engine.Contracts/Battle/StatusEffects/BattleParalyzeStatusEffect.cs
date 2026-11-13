namespace Gallean.Engine.Contracts;

public sealed record BattleParalyzeStatusEffect(int RemainingTurns) : BattleAbstractDisableEffect(RemainingTurns);
