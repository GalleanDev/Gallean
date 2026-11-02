using Avalonia.Media;

namespace Gallean.Client;

internal static partial class Icons
{
    private static readonly IconGeometry LanguagePolishIcon = CreateColored(
        viewBox: 30,
        [
            Geometry.Parse("F1 M 0 5 H 30 V 15 H 0 Z"),
            Geometry.Parse("F1 M 0 15 H 30 V 25 H 0 Z")
        ],
        MakeSolidBrush("#ffffffff"),
        MakeSolidBrush("#ffdc143c"));
}
