using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal static class ThemeAccentResources
{
    public static void Register(IResourceDictionary resources, ThemeAccent accent)
    {
        resources["AccentColor"] = accent.Primary;
        resources["AccentFadeColor"] = Fade(accent.Primary);
        resources["AccentLightColor"] = accent.PrimaryLight;
        resources["AccentLightFadeColor"] = Fade(accent.PrimaryLight);
        resources["AccentDarkColor"] = accent.PrimaryDark;
        resources["AccentDarkFadeColor"] = Fade(accent.PrimaryDark);
        resources["AccentSecondaryColor"] = accent.Secondary;
        resources["AccentSecondaryFadeColor"] = Fade(accent.Secondary);
        resources["AccentSecondaryLightColor"] = accent.SecondaryLight;
        resources["AccentSecondaryLightFadeColor"] = Fade(accent.SecondaryLight);
        resources["AccentSecondaryDarkColor"] = accent.SecondaryDark;
        resources["AccentSecondaryDarkFadeColor"] = Fade(accent.SecondaryDark);
        resources["SystemAccentColor"] = accent.Primary;
        resources["SystemAccentColorDark1"] = accent.Secondary;
        resources["SystemAccentColorDark2"] = accent.Secondary;
        resources["SystemAccentColorDark3"] = accent.Secondary;
        resources["SystemAccentColorLight1"] = accent.PrimaryLight;
        resources["SystemAccentColorLight2"] = accent.PrimaryLight;
        resources["SystemAccentColorLight3"] = accent.PrimaryLight;
    }

    private static Color Fade(Color color)
    {
        return Color.FromArgb(0, color.R, color.G, color.B);
    }
}
