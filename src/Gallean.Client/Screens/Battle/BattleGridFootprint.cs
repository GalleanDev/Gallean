using Gallean.Configuration;

namespace Gallean.Client;

internal readonly record struct BattleGridFootprint(BattlePosition BattlePosition, GridPosition Position, bool IsLarge);
