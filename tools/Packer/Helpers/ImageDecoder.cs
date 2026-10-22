using ImageMagick;

namespace Packer;

internal static class ImageDecoder
{
    public static byte[] DecodePixels(ReadOnlyMemory<byte> data, AlphaAction alpha, PixelMapping mapping)
    {
        using MagickImage image = new(data.Span);
        image.Alpha(alpha);

        using IUnsafePixelCollection<byte> pixels = image.GetPixelsUnsafe();

        return pixels.ToByteArray(mapping) ?? [];
    }
}
