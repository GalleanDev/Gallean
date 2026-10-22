using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed record AnimatedTextSegment(
    GlyphRun GlyphRun,
    IBrush? Foreground,
    Point Origin,
    TimeSpan Start,
    Vector Scatter,
    double Spin,
    Vector Seed);
