namespace Gallean.Engine;

public sealed record BattleAttackTileTarget(BattleSide Side, GridPosition Position) : BattleAttackTarget;
