using Avalonia;

namespace Gallean.Client;

internal sealed record BattleGround(Rect Bounds, int Columns, int Rows, IReadOnlyList<float> Depths, double DepthScale);
