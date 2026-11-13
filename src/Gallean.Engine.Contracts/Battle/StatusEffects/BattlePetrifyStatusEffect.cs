namespace Gallean.Engine.Contracts;

public sealed record BattlePetrifyStatusEffect(int RemainingTurns) : BattleAbstractDisableEffect(RemainingTurns);
