using Avalonia.Media;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly IconGeometry LanguageGermanIcon = CreateColored(
        viewBox: 30,
        [
            Geometry.Parse("F1 M 0 5 H 30 V 11.67 H 0 Z"),
            Geometry.Parse("F1 M 0 11.67 H 30 V 18.33 H 0 Z"),
            Geometry.Parse("F1 M 0 18.33 H 30 V 25 H 0 Z")
        ],
        MakeSolidBrush("#ff000000"),
        MakeSolidBrush("#ffdd0000"),
        MakeSolidBrush("#ffffce00"));
}
