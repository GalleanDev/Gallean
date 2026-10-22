using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal readonly record struct GalleanLogoLight(
    Point Position,
    double Height,
    double Radius,
    Color Color,
    double Intensity,
    double Specular);
