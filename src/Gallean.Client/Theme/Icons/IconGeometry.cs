using Avalonia;

namespace Gallean.Client;

internal sealed record IconGeometry(IReadOnlyList<IconLayer> Layers, double ViewBox, Point Anchor);
