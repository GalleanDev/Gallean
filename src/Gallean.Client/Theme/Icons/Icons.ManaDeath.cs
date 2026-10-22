using Avalonia;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly IconGeometry ManaDeathIcon = CreateColored(
        viewBox: 100,
        CreateManaShapes(),
        MakeLinearGradientBrush(
            new Point(34.23, 39.57),
            new Point(51.55, 74.87),
            [
                MakeGradientStop("#ff968cff", 0.0),
                MakeGradientStop("#ff8a7fff", 0.5),
                MakeGradientStop("#ff7569ff", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(35.53, 28.74),
            new Point(53.94, 36.11),
            [
                MakeGradientStop("#ffd0cbff", 0.0),
                MakeGradientStop("#ffdbd8ff", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(49.82, 15.97),
            new Point(61.73, 43.69),
            [
                MakeGradientStop("#ff8d83ff", 0.0),
                MakeGradientStop("#ff8478ff", 0.55),
                MakeGradientStop("#ff7165ea", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(64.33, 41.31),
            new Point(49.82, 83.1),
            [
                MakeGradientStop("#ff40398c", 0.0),
                MakeGradientStop("#ff282356", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(66.71, 36.54),
            new Point(59.78, 62.1),
            [
                MakeGradientStop("#ff6a5ee5", 0.0),
                MakeGradientStop("#ff49419f", 1.0)
            ]),
        MakeSolidBrush("#6be0ddff"),
        MakeSolidBrush("#427c72f5"),
        MakeLinearGradientBrush(
            new Point(29.25, 53.65),
            new Point(41.16, 83.97),
            [
                MakeGradientStop("#ff6c60ea", 0.0),
                MakeGradientStop("#ff5c52c7", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(8.02, 48.02),
            new Point(29.25, 53.22),
            [
                MakeGradientStop("#ffd6d2ff", 0.0),
                MakeGradientStop("#ffbab4ff", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(5.43, 43.04),
            new Point(15.17, 68.81),
            [
                MakeGradientStop("#ff968dff", 0.0),
                MakeGradientStop("#ff7569ff", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(16.69, 65.78),
            new Point(38.99, 83.54),
            [
                MakeGradientStop("#ff5249b2", 0.0),
                MakeGradientStop("#ff3e3787", 1.0)
            ]),
        MakeSolidBrush("#a6e0ddff"),
        MakeSolidBrush("#807c72f5"),
        MakeLinearGradientBrush(
            new Point(69.1, 54.08),
            new Point(59.13, 82.89),
            [
                MakeGradientStop("#ff7d71ff", 0.0),
                MakeGradientStop("#ff7569ff", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(69.96, 60.36),
            new Point(94.0, 42.82),
            [
                MakeGradientStop("#ffb7b1ff", 0.0),
                MakeGradientStop("#ffd4d0ff", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(93.14, 43.69),
            new Point(82.09, 69.89),
            [
                MakeGradientStop("#ff7569ff", 0.0),
                MakeGradientStop("#ff6a5ee5", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(83.17, 67.08),
            new Point(60.65, 83.32),
            [
                MakeGradientStop("#ff433c92", 0.0),
                MakeGradientStop("#ff2c2861", 1.0)
            ]),
        MakeSolidBrush("#9ee0ddff"),
        MakeSolidBrush("#757c72f5"));
}
