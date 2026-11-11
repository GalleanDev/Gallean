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
                MakeGradientStop("#ff595667", 0.0),
                MakeGradientStop("#ff454350", 0.5),
                MakeGradientStop("#ff32313a", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(35.53, 28.74),
            new Point(53.94, 36.11),
            [
                MakeGradientStop("#ffcdc7ee", 0.0),
                MakeGradientStop("#ffcdc7ee", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(49.82, 15.97),
            new Point(61.73, 43.69),
            [
                MakeGradientStop("#ff514e5e", 0.0),
                MakeGradientStop("#ff3d3b46", 0.55),
                MakeGradientStop("#ff2d2c35", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(64.33, 41.31),
            new Point(49.82, 83.1),
            [
                MakeGradientStop("#ff1c1b21", 0.0),
                MakeGradientStop("#ff141317", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(66.71, 36.54),
            new Point(59.78, 62.1),
            [
                MakeGradientStop("#ff2a2931", 0.0),
                MakeGradientStop("#ff1f1e24", 1.0)
            ]),
        MakeSolidBrush("#6bdbd4fe"),
        MakeSolidBrush("#42908ba7"),
        MakeLinearGradientBrush(
            new Point(29.25, 53.65),
            new Point(41.16, 83.97),
            [
                MakeGradientStop("#ff2b2a32", 0.0),
                MakeGradientStop("#ff25242b", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(8.02, 48.02),
            new Point(29.25, 53.22),
            [
                MakeGradientStop("#ffcdc7ee", 0.0),
                MakeGradientStop("#ffcbc5ec", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(5.43, 43.04),
            new Point(15.17, 68.81),
            [
                MakeGradientStop("#ff676478", 0.0),
                MakeGradientStop("#ff373640", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(16.69, 65.78),
            new Point(38.99, 83.54),
            [
                MakeGradientStop("#ff222128", 0.0),
                MakeGradientStop("#ff1b1a20", 1.0)
            ]),
        MakeSolidBrush("#a6dbd4fe"),
        MakeSolidBrush("#80908ba7"),
        MakeLinearGradientBrush(
            new Point(69.1, 54.08),
            new Point(59.13, 82.89),
            [
                MakeGradientStop("#ff3c3a46", 0.0),
                MakeGradientStop("#ff302f38", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(69.96, 60.36),
            new Point(94.0, 42.82),
            [
                MakeGradientStop("#ffcbc5ec", 0.0),
                MakeGradientStop("#ffcdc7ee", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(93.14, 43.69),
            new Point(82.09, 69.89),
            [
                MakeGradientStop("#ff32303a", 0.0),
                MakeGradientStop("#ff2a2931", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(83.17, 67.08),
            new Point(60.65, 83.32),
            [
                MakeGradientStop("#ff1d1c22", 0.0),
                MakeGradientStop("#ff161519", 1.0)
            ]),
        MakeSolidBrush("#9edbd4fe"),
        MakeSolidBrush("#75908ba7"));
}
