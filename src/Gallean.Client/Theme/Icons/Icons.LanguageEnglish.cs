using Avalonia.Media;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly IconGeometry LanguageEnglishIcon = CreateColored(
        viewBox: 30,
        [
            Geometry.Parse("F1 M 0 5 H 30 V 25 H 0 Z"),
            Geometry.Parse("F1 M 0 5 H 3.61 L 30 22.6 V 25 H 26.39 L 0 7.4 Z M 30 5 V 7.4 L 3.61 25 H 0 V 22.6 L 26.39 5 Z"),
            Geometry.Parse("F1 M 0 5 H 1.2 L 30 24.2 V 25 H 28.8 L 0 5.8 Z M 30 5 V 5.8 L 1.2 25 H 0 V 24.2 L 28.8 5 Z"),
            Geometry.Parse("F1 M 11.67 5 H 18.33 V 11.67 H 30 V 18.33 H 18.33 V 25 H 11.67 V 18.33 H 0 V 11.67 H 11.67 Z"),
            Geometry.Parse("F1 M 13 5 H 17 V 13 H 30 V 17 H 17 V 25 H 13 V 17 H 0 V 13 H 13 Z")
        ],
        MakeSolidBrush("#ff012169"),
        MakeSolidBrush("#ffffffff"),
        MakeSolidBrush("#ffc8102e"),
        MakeSolidBrush("#ffffffff"),
        MakeSolidBrush("#ffc8102e"));
}
