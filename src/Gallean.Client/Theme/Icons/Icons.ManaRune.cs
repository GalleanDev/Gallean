using Avalonia;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly IconGeometry ManaRuneIcon = CreateColored(
        viewBox: 100,
        CreateManaShapes(),
        MakeLinearGradientBrush(
            new Point(34.23, 39.57),
            new Point(51.55, 74.87),
            [
                MakeGradientStop("#ff39d7f4", 0.0),
                MakeGradientStop("#ff24c2f6", 0.5),
                MakeGradientStop("#ff00a2f5", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(35.53, 28.74),
            new Point(53.94, 36.11),
            [
                MakeGradientStop("#ffa5f6fc", 0.0),
                MakeGradientStop("#ffb9f5fa", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(49.82, 15.97),
            new Point(61.73, 43.69),
            [
                MakeGradientStop("#ff2ad7f4", 0.0),
                MakeGradientStop("#ff19b6f5", 0.55),
                MakeGradientStop("#ff098cf8", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(64.33, 41.31),
            new Point(49.82, 83.1),
            [
                MakeGradientStop("#ff0046d2", 0.0),
                MakeGradientStop("#ff002798", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(66.71, 36.54),
            new Point(59.78, 62.1),
            [
                MakeGradientStop("#ff0087f5", 0.0),
                MakeGradientStop("#ff0054db", 1.0)
            ]),
        MakeSolidBrush("#6bc5faff"),
        MakeSolidBrush("#42159ceb"),
        MakeLinearGradientBrush(
            new Point(29.25, 53.65),
            new Point(41.16, 83.97),
            [
                MakeGradientStop("#ff008af5", 0.0),
                MakeGradientStop("#ff0070ee", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(8.02, 48.02),
            new Point(29.25, 53.22),
            [
                MakeGradientStop("#ffaff8fa", 0.0),
                MakeGradientStop("#ff7be3f6", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(5.43, 43.04),
            new Point(15.17, 68.81),
            [
                MakeGradientStop("#ff3adbf2", 0.0),
                MakeGradientStop("#ff00b4ee", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(16.69, 65.78),
            new Point(38.99, 83.54),
            [
                MakeGradientStop("#ff0061e4", 0.0),
                MakeGradientStop("#ff0042d2", 1.0)
            ]),
        MakeSolidBrush("#a6c5faff"),
        MakeSolidBrush("#80159ceb"),
        MakeLinearGradientBrush(
            new Point(69.1, 54.08),
            new Point(59.13, 82.89),
            [
                MakeGradientStop("#ff0dbbf4", 0.0),
                MakeGradientStop("#ff009cf1", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(69.96, 60.36),
            new Point(94.0, 42.82),
            [
                MakeGradientStop("#ff76e1f6", 0.0),
                MakeGradientStop("#ffadf8fb", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(93.14, 43.69),
            new Point(82.09, 69.89),
            [
                MakeGradientStop("#ff009ff8", 0.0),
                MakeGradientStop("#ff0087f2", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(83.17, 67.08),
            new Point(60.65, 83.32),
            [
                MakeGradientStop("#ff004bd3", 0.0),
                MakeGradientStop("#ff002caa", 1.0)
            ]),
        MakeSolidBrush("#9ec5faff"),
        MakeSolidBrush("#75159ceb"));
}
