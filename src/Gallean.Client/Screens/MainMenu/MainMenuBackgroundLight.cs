using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed record MainMenuBackgroundLight(
    Point Center,
    Vector Sway,
    TimeSpan SwayPeriod,
    double SwayPhase,
    double Height,
    double Radius,
    double CoreRadius,
    Color Color,
    double Power,
    double Flicker,
    double FlickerRate);
