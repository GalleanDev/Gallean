using Avalonia.Platform;
using SkiaSharp;

namespace Gallean.Client;

internal sealed record GalleanLogoLayers(SKImage Color, SKImage Normal)
{
    private const string ColorPath = "avares://Gallean.Client/Theme/Assets/GalleanLogoColor.png";
    private const string NormalPath = "avares://Gallean.Client/Theme/Assets/GalleanLogoNormal.png";

    private static readonly Lazy<GalleanLogoLayers> Instance = new(Load);

    public static GalleanLogoLayers Default => Instance.Value;

    private static GalleanLogoLayers Load()
    {
        return new GalleanLogoLayers(Decode(ColorPath), Decode(NormalPath));
    }

    private static SKImage Decode(string path)
    {
        using Stream stream = AssetLoader.Open(new Uri(path));
        using SKData data = SKData.Create(stream);
        SKImage encodedImage = SKImage.FromEncodedData(data);
        SKImage rasterImage = encodedImage.ToRasterImage(true);

        if (!ReferenceEquals(rasterImage, encodedImage))
        {
            encodedImage.Dispose();
        }

        return rasterImage;
    }
}
