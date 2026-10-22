using Avalonia;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly IconGeometry ManaLifeIcon = CreateColored(
        viewBox: 100,
        CreateManaShapes(),
        MakeLinearGradientBrush(
            new Point(34.23, 39.57),
            new Point(51.55, 74.87),
            [
                MakeGradientStop("#ffe5e5e8", 0.0),
                MakeGradientStop("#ffcccccf", 0.5),
                MakeGradientStop("#ffa4a4a7", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(35.53, 28.74),
            new Point(53.94, 36.11),
            [
                MakeGradientStop("#fffefeff", 0.0),
                MakeGradientStop("#fffefeff", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(49.82, 15.97),
            new Point(61.73, 43.69),
            [
                MakeGradientStop("#ffdfdfe2", 0.0),
                MakeGradientStop("#ffbebec1", 0.55),
                MakeGradientStop("#ff979799", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(64.33, 41.31),
            new Point(49.82, 83.1),
            [
                MakeGradientStop("#ff575758", 0.0),
                MakeGradientStop("#ff353536", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(66.71, 36.54),
            new Point(59.78, 62.1),
            [
                MakeGradientStop("#ff8f8f91", 0.0),
                MakeGradientStop("#ff636365", 1.0)
            ]),
        MakeSolidBrush("#6bfefeff"),
        MakeSolidBrush("#42a6a6a9"),
        MakeLinearGradientBrush(
            new Point(29.25, 53.65),
            new Point(41.16, 83.97),
            [
                MakeGradientStop("#ff919193", 0.0),
                MakeGradientStop("#ff7c7c7e", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(8.02, 48.02),
            new Point(29.25, 53.22),
            [
                MakeGradientStop("#fffefeff", 0.0),
                MakeGradientStop("#fffdfdff", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(5.43, 43.04),
            new Point(15.17, 68.81),
            [
                MakeGradientStop("#ffe8e8eb", 0.0),
                MakeGradientStop("#ffb1b1b4", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(16.69, 65.78),
            new Point(38.99, 83.54),
            [
                MakeGradientStop("#ff6f6f70", 0.0),
                MakeGradientStop("#ff535355", 1.0)
            ]),
        MakeSolidBrush("#a6fefeff"),
        MakeSolidBrush("#80a6a6a9"),
        MakeLinearGradientBrush(
            new Point(69.1, 54.08),
            new Point(59.13, 82.89),
            [
                MakeGradientStop("#ffbdbdc0", 0.0),
                MakeGradientStop("#ff9f9fa2", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(69.96, 60.36),
            new Point(94.0, 42.82),
            [
                MakeGradientStop("#fffdfdff", 0.0),
                MakeGradientStop("#fffefeff", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(93.14, 43.69),
            new Point(82.09, 69.89),
            [
                MakeGradientStop("#ffa2a2a5", 0.0),
                MakeGradientStop("#ff8f8f91", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(83.17, 67.08),
            new Point(60.65, 83.32),
            [
                MakeGradientStop("#ff5b5b5d", 0.0),
                MakeGradientStop("#ff3c3c3d", 1.0)
            ]),
        MakeSolidBrush("#9efefeff"),
        MakeSolidBrush("#75a6a6a9"));
}
