using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed record AnimatedTextSegmentFrame(
    GlyphRun GlyphRun,
    Color Foreground,
    Vector Seed,
    Matrix Transform,
    double Opacity,
    double Blur,
    double Dissolve,
    double Tint,
    double Glow,
    double NoiseSize);
