using Avalonia;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly IconGeometry ManaInfernalIcon = CreateColored(
        viewBox: 100,
        CreateManaShapes(),
        MakeLinearGradientBrush(
            new Point(34.23, 39.57),
            new Point(51.55, 74.87),
            [
                MakeGradientStop("#ffff3c3f", 0.0),
                MakeGradientStop("#ffff2529", 0.5),
                MakeGradientStop("#ffff0004", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(35.53, 28.74),
            new Point(53.94, 36.11),
            [
                MakeGradientStop("#ffffa7a8", 0.0),
                MakeGradientStop("#ffffbdbe", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(49.82, 15.97),
            new Point(61.73, 43.69),
            [
                MakeGradientStop("#ffff2c2f", 0.0),
                MakeGradientStop("#ffff1a1e", 0.55),
                MakeGradientStop("#ffff090d", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(64.33, 41.31),
            new Point(49.82, 83.1),
            [
                MakeGradientStop("#ffd80003", 0.0),
                MakeGradientStop("#ff850002", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(66.71, 36.54),
            new Point(59.78, 62.1),
            [
                MakeGradientStop("#ffff0004", 0.0),
                MakeGradientStop("#fff60004", 1.0)
            ]),
        MakeSolidBrush("#6bffc5c6"),
        MakeSolidBrush("#42ff171b"),
        MakeLinearGradientBrush(
            new Point(29.25, 53.65),
            new Point(41.16, 83.97),
            [
                MakeGradientStop("#ffff0004", 0.0),
                MakeGradientStop("#ffff0004", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(8.02, 48.02),
            new Point(29.25, 53.22),
            [
                MakeGradientStop("#ffffb2b4", 0.0),
                MakeGradientStop("#ffff7f82", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(5.43, 43.04),
            new Point(15.17, 68.81),
            [
                MakeGradientStop("#ffff3d40", 0.0),
                MakeGradientStop("#ffff0004", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(16.69, 65.78),
            new Point(38.99, 83.54),
            [
                MakeGradientStop("#ffff0004", 0.0),
                MakeGradientStop("#ffd00003", 1.0)
            ]),
        MakeSolidBrush("#a6ffc5c6"),
        MakeSolidBrush("#80ff171b"),
        MakeLinearGradientBrush(
            new Point(69.1, 54.08),
            new Point(59.13, 82.89),
            [
                MakeGradientStop("#ffff0e11", 0.0),
                MakeGradientStop("#ffff0004", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(69.96, 60.36),
            new Point(94.0, 42.82),
            [
                MakeGradientStop("#ffff7a7c", 0.0),
                MakeGradientStop("#ffffb0b1", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(93.14, 43.69),
            new Point(82.09, 69.89),
            [
                MakeGradientStop("#ffff0004", 0.0),
                MakeGradientStop("#ffff0004", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(83.17, 67.08),
            new Point(60.65, 83.32),
            [
                MakeGradientStop("#ffe20004", 0.0),
                MakeGradientStop("#ff960002", 1.0)
            ]),
        MakeSolidBrush("#9effc5c6"),
        MakeSolidBrush("#75ff171b"));
}
