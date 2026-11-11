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
                MakeGradientStop("#ffff603c", 0.0),
                MakeGradientStop("#ffff4d25", 0.5),
                MakeGradientStop("#ffff2f00", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(35.53, 28.74),
            new Point(53.94, 36.11),
            [
                MakeGradientStop("#ffffb8a7", 0.0),
                MakeGradientStop("#ffffc9bd", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(49.82, 15.97),
            new Point(61.73, 43.69),
            [
                MakeGradientStop("#ffff532c", 0.0),
                MakeGradientStop("#ffff441a", 0.55),
                MakeGradientStop("#ffff3609", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(64.33, 41.31),
            new Point(49.82, 83.1),
            [
                MakeGradientStop("#ffd82800", 0.0),
                MakeGradientStop("#ff851900", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(66.71, 36.54),
            new Point(59.78, 62.1),
            [
                MakeGradientStop("#ffff2f00", 0.0),
                MakeGradientStop("#fff62d00", 1.0)
            ]),
        MakeSolidBrush("#6bffd0c5"),
        MakeSolidBrush("#42ff4117"),
        MakeLinearGradientBrush(
            new Point(29.25, 53.65),
            new Point(41.16, 83.97),
            [
                MakeGradientStop("#ffff2f00", 0.0),
                MakeGradientStop("#ffff2f00", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(8.02, 48.02),
            new Point(29.25, 53.22),
            [
                MakeGradientStop("#ffffbfb2", 0.0),
                MakeGradientStop("#ffff967f", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(5.43, 43.04),
            new Point(15.17, 68.81),
            [
                MakeGradientStop("#ffff613d", 0.0),
                MakeGradientStop("#ffff2f00", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(16.69, 65.78),
            new Point(38.99, 83.54),
            [
                MakeGradientStop("#ffff2f00", 0.0),
                MakeGradientStop("#ffd02700", 1.0)
            ]),
        MakeSolidBrush("#a6ffd0c5"),
        MakeSolidBrush("#80ff4117"),
        MakeLinearGradientBrush(
            new Point(69.1, 54.08),
            new Point(59.13, 82.89),
            [
                MakeGradientStop("#ffff3b0e", 0.0),
                MakeGradientStop("#ffff2f00", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(69.96, 60.36),
            new Point(94.0, 42.82),
            [
                MakeGradientStop("#ffff937a", 0.0),
                MakeGradientStop("#ffffbfb0", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(93.14, 43.69),
            new Point(82.09, 69.89),
            [
                MakeGradientStop("#ffff2f00", 0.0),
                MakeGradientStop("#ffff2f00", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(83.17, 67.08),
            new Point(60.65, 83.32),
            [
                MakeGradientStop("#ffe22900", 0.0),
                MakeGradientStop("#ff961c00", 1.0)
            ]),
        MakeSolidBrush("#9effd0c5"),
        MakeSolidBrush("#75ff4117"));
}
