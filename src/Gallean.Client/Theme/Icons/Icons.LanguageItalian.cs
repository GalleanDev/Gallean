using Avalonia.Media;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly IconGeometry LanguageItalianIcon = CreateColored(
        viewBox: 30,
        [
            Geometry.Parse("F1 M 0 5 H 10 V 25 H 0 Z"),
            Geometry.Parse("F1 M 10 5 H 20 V 25 H 10 Z"),
            Geometry.Parse("F1 M 20 5 H 30 V 25 H 20 Z")
        ],
        MakeSolidBrush("#ff009246"),
        MakeSolidBrush("#ffffffff"),
        MakeSolidBrush("#ffce2b37"));
}
