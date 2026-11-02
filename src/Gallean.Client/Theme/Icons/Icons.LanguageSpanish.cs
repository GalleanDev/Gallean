using Avalonia.Media;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly IconGeometry LanguageSpanishIcon = CreateColored(
        viewBox: 30,
        [
            Geometry.Parse("F1 M 0 5 H 30 V 10 H 0 Z"),
            Geometry.Parse("F1 M 0 10 H 30 V 20 H 0 Z"),
            Geometry.Parse("F1 M 0 20 H 30 V 25 H 0 Z")
        ],
        MakeSolidBrush("#ffaa151b"),
        MakeSolidBrush("#fff1bf00"),
        MakeSolidBrush("#ffaa151b"));
}
