using Avalonia;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly IconGeometry ManaGroveIcon = CreateColored(
        viewBox: 100,
        CreateManaShapes(),
        MakeLinearGradientBrush(
            new Point(34.23, 39.57),
            new Point(51.55, 74.87),
            [
                MakeGradientStop("#ff6fde59", 0.0),
                MakeGradientStop("#ff5acd44", 0.5),
                MakeGradientStop("#ff3caf26", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(35.53, 28.74),
            new Point(53.94, 36.11),
            [
                MakeGradientStop("#ffc2fab7", 0.0),
                MakeGradientStop("#ffcffac7", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(49.82, 15.97),
            new Point(61.73, 43.69),
            [
                MakeGradientStop("#ff65dd4e", 0.0),
                MakeGradientStop("#ff50c23a", 0.55),
                MakeGradientStop("#ff3a9e27", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(64.33, 41.31),
            new Point(49.82, 83.1),
            [
                MakeGradientStop("#ff205c14", 0.0),
                MakeGradientStop("#ff14390c", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(66.71, 36.54),
            new Point(59.78, 62.1),
            [
                MakeGradientStop("#ff349821", 0.0),
                MakeGradientStop("#ff246917", 1.0)
            ]),
        MakeSolidBrush("#6bd9ffd2"),
        MakeSolidBrush("#4245ab31"),
        MakeLinearGradientBrush(
            new Point(29.25, 53.65),
            new Point(41.16, 83.97),
            [
                MakeGradientStop("#ff359b22", 0.0),
                MakeGradientStop("#ff2d841d", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(8.02, 48.02),
            new Point(29.25, 53.22),
            [
                MakeGradientStop("#ffcafbc1", 0.0),
                MakeGradientStop("#ff9eea8f", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(5.43, 43.04),
            new Point(15.17, 68.81),
            [
                MakeGradientStop("#ff71e15c", 0.0),
                MakeGradientStop("#ff41bc29", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(16.69, 65.78),
            new Point(38.99, 83.54),
            [
                MakeGradientStop("#ff28761a", 0.0),
                MakeGradientStop("#ff1f5913", 1.0)
            ]),
        MakeSolidBrush("#a6d9ffd2"),
        MakeSolidBrush("#8045ab31"),
        MakeLinearGradientBrush(
            new Point(69.1, 54.08),
            new Point(59.13, 82.89),
            [
                MakeGradientStop("#ff4ac533", 0.0),
                MakeGradientStop("#ff3aa925", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(69.96, 60.36),
            new Point(94.0, 42.82),
            [
                MakeGradientStop("#ff9ae98b", 0.0),
                MakeGradientStop("#ffc8fcbf", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(93.14, 43.69),
            new Point(82.09, 69.89),
            [
                MakeGradientStop("#ff3bad26", 0.0),
                MakeGradientStop("#ff349821", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(83.17, 67.08),
            new Point(60.65, 83.32),
            [
                MakeGradientStop("#ff216115", 0.0),
                MakeGradientStop("#ff16400e", 1.0)
            ]),
        MakeSolidBrush("#9ed9ffd2"),
        MakeSolidBrush("#7545ab31"));
}
