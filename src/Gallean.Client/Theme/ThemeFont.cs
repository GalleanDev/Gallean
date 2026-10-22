using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Gallean.Client;

internal static class ThemeFont
{
    private const string RegularSource = "avares://Gallean.Client/Theme/Assets/AlegreyaSans-Regular.ttf";
    private const string BoldSource = "avares://Gallean.Client/Theme/Assets/AlegreyaSans-Bold.ttf";

    public static FontFeatureCollection Features { get; } = [new FontFeature { Tag = "lnum" }, new FontFeature { Tag = "tnum" }];

    public static SKTypeface Regular { get; } = Load(RegularSource);
    public static SKTypeface Bold { get; } = Load(BoldSource);

    private static SKTypeface Load(string source)
    {
        using Stream stream = AssetLoader.Open(new Uri(source));

        return SKTypeface.FromStream(stream);
    }
}
