using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly Geometry[] GoldShapes =
    [
        Geometry.Parse(
            "F1 M 5.01 52.74 C 5.01 47.89 17.75 44.2 33.49 44.16 C 48.88 44.11 61.46 47.59 62.28 52.46 L 62.5 65.8 C " +
            "62.11 70.59 49.39 74.57 33.64 74.74 C 18.35 74.88 5.49 70.82 5.16 66.19 Z"),
        Geometry.Parse(
            "F1 M 5.08 54.18 C 6.72 56.12 9.93 57.52 13.94 58.64 L 13.92 72.03 C 9.06 70.59 5.44 68.39 5.16 66.19 Z"),
        Geometry.Parse(
            "F1 M 13.94 58.64 C 18.76 59.96 24.76 60.69 31.22 60.93 L 31.22 74.7 C 24.78 74.6 18.77 73.65 13.92 72.03 Z"),
        Geometry.Parse(
            "F1 M 51.38 58.97 C 57.72 57.22 61.01 55.33 62.29 53.08 L 62.5 65.8 C 62.2 68.5 57.95 70.95 51.38 72.76 Z"),
        Geometry.Parse(
            "F1 M 62.29 52.37 C 62.71 57.05 50.13 60.8 34.31 61.01 C 18.53 61.23 5.4 57.65 5.01 52.93 C 4.64 48.21 17.17 " +
            "44.29 32.99 44.09 C 48.81 43.86 61.9 47.65 62.29 52.37 Z"),
        Geometry.Parse(
            "F1 M 6 54.46 C 10.04 58.56 22.84 60.91 34.76 60.6 C 46.38 60.48 56.14 58.32 60.13 55.47 C 56.51 58.94 46.16 " +
            "61.01 34.31 61.01 C 21.55 61.19 9.39 58.73 6 54.46 Z"),
        Geometry.Parse(
            "F1 M 11.78 40.69 C 13.57 36.96 23.55 34.52 35.64 34.52 C 48.42 34.52 58.19 37.65 58.49 41.25 L 58.58 51.08 " +
            "C 57.41 54.96 47.28 57.97 35.56 57.97 C 23.7 57.97 12.54 55.24 11.82 51.34 Z"),
        Geometry.Parse(
            "F1 M 11.98 46.32 L 14.47 47.74 L 14.5 54.2 C 12.92 53.36 11.97 52.46 11.82 51.34 Z"),
        Geometry.Parse(
            "F1 M 14.56 47.71 L 21.39 49.66 L 21.39 56.4 C 18.74 55.86 16.29 55.11 14.5 54.2 Z"),
        Geometry.Parse(
            "F1 M 49.89 48.08 L 56.34 45.95 L 57.72 52.48 C 55.97 54.09 53.42 55.15 49.91 56.19 Z"),
        Geometry.Parse(
            "F1 M 11.82 44.93 C 19.09 49.27 27 49.38 35.82 49.05 C 43.72 48.77 50.77 46.96 56.01 44.4 L 56.57 46.9 C " +
            "50.41 50 42.48 51.36 34.28 51.38 C 24.5 51.38 16.22 49.94 11.82 47.24 Z"),
        Geometry.Parse(
            "F1 M 9.43 32.41 L 9.49 42.28 C 9.84 46.9 21.83 50 36.18 49.48 C 49.83 48.99 60.32 45.37 60.91 40.95 L 61.12 " +
            "29 Z"),
        Geometry.Parse(
            "F1 M 49.29 35.58 L 54.5 33.57 L 54.39 45.34 C 52.89 46.06 51.03 46.75 49.27 47.22 Z"),
        Geometry.Parse(
            "F1 M 61.1 28.77 C 61.49 32.88 50.49 37.18 36.22 38.36 C 21.96 39.52 9.91 37.15 9.43 32.84 C 8.94 28.49 " +
            "20.14 24.28 34.35 23.21 C 48.6 22.15 60.69 24.48 61.1 28.77 Z"),
        Geometry.Parse(
            "F1 M 57.05 28.7 C 57.35 31.85 48.19 35.08 36.46 36.03 C 24.95 36.98 15.27 34.89 14.97 31.83 C 14.65 28.62 " +
            "23.74 25.32 35.36 24.35 C 47.02 23.36 56.75 25.49 57.05 28.7 Z"),
        Geometry.Parse(
            "F1 M 56.75 29.31 C 55.8 32.32 47.26 35.17 36.46 36.03 C 25.88 36.9 17 35.24 15.27 32.5 C 15.81 29.63 24.76 " +
            "26.16 35.9 25.21 C 46.33 24.35 55 26.14 56.75 29.31 Z"),
        Geometry.Parse(
            "F1 M 9.54 33.32 C 11.63 36.94 22.92 39.05 36.2 38.01 C 47.41 37.13 56.79 34.14 60.07 30.86 C 57.56 34.5 " +
            "47.69 37.41 36.22 38.36 C 22.71 39.46 11.18 37.35 9.54 33.32 Z"),
        Geometry.Parse(
            "F1 M 64.14 27.63 L 57.39 32.73 C 50.71 37.59 52.48 48.26 59.2 60.02 C 66.08 72.05 76.6 79.62 82.16 75.87 L " +
            "92.14 69.59 Z"),
        Geometry.Parse(
            "F1 M 57.39 32.73 L 64.14 27.63 L 63.1 38.53 L 53.94 45.08 C 52.39 39.59 53.62 35.52 57.39 32.73 Z"),
        Geometry.Parse(
            "F1 M 68.23 70.72 L 76.17 64.27 L 83.9 71 L 77.48 76.95 C 74.27 76.25 71.21 73.9 68.23 70.72 Z"),
        Geometry.Parse(
            "F1 M 77.48 76.95 L 83.9 71 L 92.14 69.59 L 82.16 75.87 C 80.84 76.77 79.53 77.25 77.48 76.95 Z"),
        Geometry.Parse(
            "F1 M 64.22 27.65 C 70.13 24.41 80.74 31.46 88.09 43.28 C 95.24 54.79 97.16 67.01 91.97 70.67 C 86.64 74.34 " +
            "76.06 67.39 68.47 55.63 C 60.73 43.69 58.81 30.62 64.22 27.65 Z"),
        Geometry.Parse(
            "F1 M 67.93 31.42 C 72.18 28.62 80.69 34.35 86.88 44.14 C 93.07 53.94 94.16 63.79 89.88 66.28 C 85.59 68.78 " +
            "77.27 63.11 71.27 53.58 C 65.17 43.92 64.18 33.96 67.93 31.42 Z"),
        Geometry.Parse(
            "F1 M 66.51 33.55 C 70.58 30.36 78.78 36.29 85.05 45.91 C 90.67 54.55 92.03 62.67 89.88 66.28 C 85.59 68.78 " +
            "77.27 63.11 71.27 53.58 C 66.49 46.03 64.81 38.14 66.51 33.55 Z"),
        Geometry.Parse(
            "F1 M 64.22 27.65 C 59.48 31.22 61.68 43.73 69.07 55.24 C 76.45 66.68 86.66 73.69 91.97 70.67 C 87.01 74.62 " +
            "76.02 67.94 68.1 55.39 C 60.5 43.4 58.84 30.69 64.22 27.65 Z"),
        Geometry.Parse(
            "F1 M 70.67 27.82 C 76.32 30.13 82.8 35.86 87.7 43.69 C 94.21 54.14 96.66 65.45 92.7 69.89 C 97.23 65.5 " +
            "94.73 54.09 88.09 43.28 C 82.89 34.95 76.27 29.31 70.67 27.82 Z")
    ];

    private static readonly IconGeometry GoldIcon = CreateColored(
        viewBox: 100,
        GoldShapes,
        MakeLinearGradientBrush(
            new Point(5.23, 62.48),
            new Point(62.31, 62.48),
            [
                MakeGradientStop("#ffef9b00", 0.0),
                MakeGradientStop("#ffffd82c", 0.17),
                MakeGradientStop("#ffffc71c", 0.4),
                MakeGradientStop("#ffdc8000", 0.7),
                MakeGradientStop("#ff914000", 1.0)
            ]),
        MakeSolidBrush("#57cb800e"),
        MakeSolidBrush("#40ffd957"),
        MakeSolidBrush("#4f8c4508"),
        MakeLinearGradientBrush(
            new Point(10.27, 58.56),
            new Point(55.6, 46.81),
            [
                MakeGradientStop("#fffff58b", 0.0),
                MakeGradientStop("#ffffff8b", 0.34),
                MakeGradientStop("#ffffcf29", 0.65),
                MakeGradientStop("#ffd18000", 1.0)
            ]),
        MakeSolidBrush("#bffff0a1"),
        MakeLinearGradientBrush(
            new Point(11.76, 51.1),
            new Point(57.46, 51.1),
            [
                MakeGradientStop("#ffa95500", 0.0),
                MakeGradientStop("#fff0a000", 0.12),
                MakeGradientStop("#ffffc32a", 0.32),
                MakeGradientStop("#ffefa20a", 0.58),
                MakeGradientStop("#ffc26c00", 0.83),
                MakeGradientStop("#ff843500", 1.0)
            ]),
        MakeSolidBrush("#5e8c4508"),
        MakeSolidBrush("#59ffd957"),
        MakeSolidBrush("#5c8c4508"),
        MakeSolidBrush("#cc8c4508"),
        MakeLinearGradientBrush(
            new Point(9.52, 42.15),
            new Point(61.01, 41.21),
            [
                MakeGradientStop("#ffffbd0f", 0.0),
                MakeGradientStop("#ffffd32b", 0.13),
                MakeGradientStop("#ffffed65", 0.13),
                MakeGradientStop("#ffffcb21", 0.36),
                MakeGradientStop("#ffe59400", 0.63),
                MakeGradientStop("#ffb76200", 0.78),
                MakeGradientStop("#ff914100", 1.0)
            ]),
        MakeSolidBrush("#6b8c4508"),
        MakeLinearGradientBrush(
            new Point(15.31, 36.92),
            new Point(55.41, 23.49),
            [
                MakeGradientStop("#ffffff98", 0.0),
                MakeGradientStop("#fffff46b", 0.29),
                MakeGradientStop("#ffffd342", 0.61),
                MakeGradientStop("#ffe89409", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(15.12, 29.09),
            new Point(57.09, 29.09),
            [
                MakeGradientStop("#ffed9700", 0.0),
                MakeGradientStop("#ffefa016", 0.4),
                MakeGradientStop("#ffbf6a00", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(19.04, 35.06),
            new Point(55.6, 26.29),
            [
                MakeGradientStop("#fffff468", 0.0),
                MakeGradientStop("#ffffcf36", 0.35),
                MakeGradientStop("#fff2a414", 0.72),
                MakeGradientStop("#ffcc7a03", 1.0)
            ]),
        MakeSolidBrush("#fffff0a1"),
        MakeLinearGradientBrush(
            new Point(55.78, 41.21),
            new Point(85.07, 74.42),
            [
                MakeGradientStop("#fff7b80d", 0.0),
                MakeGradientStop("#ffffda36", 0.24),
                MakeGradientStop("#ffeaa006", 0.49),
                MakeGradientStop("#ffb05b00", 0.76),
                MakeGradientStop("#ff9a4600", 1.0)
            ]),
        MakeSolidBrush("#57cb800e"),
        MakeSolidBrush("#668c4508"),
        MakeSolidBrush("#6ecb800e"),
        MakeLinearGradientBrush(
            new Point(63.81, 50.73),
            new Point(83.21, 42.89),
            [
                MakeGradientStop("#ffffff91", 0.0),
                MakeGradientStop("#ffffe34a", 0.35),
                MakeGradientStop("#ffffc52c", 0.72),
                MakeGradientStop("#fff6ae1b", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(67.35, 31.89),
            new Point(92.53, 65.09),
            [
                MakeGradientStop("#ffd78a02", 0.0),
                MakeGradientStop("#ffb96300", 0.36),
                MakeGradientStop("#ffc87500", 0.7),
                MakeGradientStop("#ffea9b09", 1.0)
            ]),
        MakeLinearGradientBrush(
            new Point(67.35, 45.32),
            new Point(89.73, 51.85),
            [
                MakeGradientStop("#ffffdf39", 0.0),
                MakeGradientStop("#ffffca20", 0.32),
                MakeGradientStop("#fff4a300", 0.7),
                MakeGradientStop("#ffdf8900", 1.0)
            ]),
        MakeSolidBrush("#fffff0a1"),
        MakeSolidBrush("#d9ffd957"));
}
