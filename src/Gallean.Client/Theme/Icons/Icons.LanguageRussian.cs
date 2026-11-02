using Avalonia.Media;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly IconGeometry LanguageRussianIcon = CreateColored(
        viewBox: 30,
        [
            Geometry.Parse("F1 M 0 5 H 30 V 11.67 H 0 Z"),
            Geometry.Parse("F1 M 0 11.67 H 30 V 18.33 H 0 Z"),
            Geometry.Parse("F1 M 0 18.33 H 30 V 25 H 0 Z")
        ],
        MakeSolidBrush("#ffffffff"),
        MakeSolidBrush("#ff0039a6"),
        MakeSolidBrush("#ffd52b1e"));
}
