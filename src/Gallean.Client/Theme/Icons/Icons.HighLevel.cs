using Avalonia.Media;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly IconGeometry HighLevelIcon = new(
        [
            new IconLayer(
                Geometry.Parse("F1 M 50 4 L 54 12 L 54 59.5 L 46 59.5 L 46 12 Z"),
                null),
            new IconLayer(
                Geometry.Parse(
                    "F1 M 29 53 C 31 52.5 32.5 54 33 56 C 34 58.5 40 59.5 50 59.5 C 60 59.5 66 58.5 67 56 C 67.5 54 69 52.5 71 53 C 71.5 58 69 63.5 60 64.5 " +
                    "L 50 65.5 L 40 64.5 C 31 63.5 28.5 58 29 53 Z M 47 66 L 53 66 L 52.5 82 L 47.5 82 Z M 44.5 88 A 5.5 5.5 0 1 0 55.5 88 A 5.5 5.5 0 1 0 44.5 88 Z"),
                MakeSolidBrush("#f6d692"))
        ],
        100,
        GetCenter(100));
}
