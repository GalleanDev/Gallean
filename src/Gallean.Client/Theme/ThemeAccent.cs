using Avalonia.Media;

namespace Gallean.Client;

internal sealed record ThemeAccent(
    Color Primary,
    Color PrimaryLight,
    Color PrimaryDark,
    Color Secondary,
    Color SecondaryLight,
    Color SecondaryDark);
