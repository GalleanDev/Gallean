using System.Globalization;
using ImageMagick;

namespace Packer;

internal static class WebpEncoder
{
    public static AssetPack.Writer.EncodedImage Encode(
        ReadOnlySpan<byte> pixels,
        int pixelWidth,
        int pixelHeight,
        WebpSettings settings)
    {
        PixelReadSettings pixelReadSettings = new(
            (uint)pixelWidth,
            (uint)pixelHeight,
            StorageType.Char,
            PixelMapping.RGBA);

        using MagickImage magick = new(pixels, pixelReadSettings);
        magick.Format = MagickFormat.WebP;
        Configure(magick, settings);

        return new AssetPack.Writer.EncodedImage(pixelWidth, pixelHeight, magick.ToByteArray());
    }

    public static byte[] EncodeAnimation(
        IReadOnlyList<byte[]> frames,
        int pixelWidth,
        int pixelHeight,
        TimeSpan frameDuration,
        WebpSettings settings)
    {
        PixelReadSettings pixelReadSettings = new(
            (uint)pixelWidth,
            (uint)pixelHeight,
            StorageType.Char,
            PixelMapping.RGBA);

        using MagickImageCollection collection = [];

        foreach (byte[] pixels in frames)
        {
            MagickImage magick = new(pixels, pixelReadSettings);
            magick.Format = MagickFormat.WebP;
            magick.AnimationTicksPerSecond = 1000;
            magick.AnimationDelay = (uint)frameDuration.TotalMilliseconds;
            Configure(magick, settings);

            collection.Add(magick);
        }

        return collection.ToByteArray(MagickFormat.WebP);
    }

    private static void Configure(MagickImage magick, WebpSettings settings)
    {
        magick.Settings.SetDefine(MagickFormat.WebP, "lossless", settings.Lossless ? "true" : "false");
        magick.Settings.SetDefine(MagickFormat.WebP, "exact", settings.Exact ? "true" : "false");
        magick.Settings.SetDefine(MagickFormat.WebP, "method", settings.Method.ToString(CultureInfo.InvariantCulture));
        magick.Settings.SetDefine(MagickFormat.WebP, "use-sharp-yuv", settings.SharpYuv ? "true" : "false");
        magick.Quality = (uint)settings.Quality;
    }
}
