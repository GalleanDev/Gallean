using SkiaSharp;

namespace Gallean.Client;

internal sealed class AssetImageDecoder : IAssetDecoder<SKImage>
{
    private volatile int maxPixelScale = int.MaxValue;

    public int MaxPixelScale
    {
        get => maxPixelScale;
        set => maxPixelScale = value;
    }

    public AssetSprite<SKImage> DecodeFrame(AssetSpriteMetadata metadata, ReadOnlyMemory<byte> data)
    {
        AssetSpriteMetadata decoded = metadata with
        {
            PixelWidth = LimitPixelScale(metadata.PixelWidth, metadata.PixelScale),
            PixelHeight = LimitPixelScale(metadata.PixelHeight, metadata.PixelScale)
        };

        return new AssetSprite<SKImage>(decoded, DecodeImage(data, decoded.PixelWidth, decoded.PixelHeight, premultiply: !metadata.Additive));
    }

    public AssetSpriteLayer<SKImage> DecodeLayer(AssetSpriteMetadata frame, int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> data)
    {
        int decodedWidth = LimitPixelScale(pixelWidth, frame.PixelScale);
        int decodedHeight = LimitPixelScale(pixelHeight, frame.PixelScale);

        return new AssetSpriteLayer<SKImage>(decodedWidth, decodedHeight, DecodeImage(data, decodedWidth, decodedHeight, premultiply: true));
    }

    private int LimitPixelScale(int pixels, int pixelScale)
    {
        int limit = maxPixelScale;

        if (pixelScale > limit)
        {
            return Math.Max(1, pixels * limit / pixelScale);
        }
        else
        {
            return pixels;
        }
    }

    private static SKImage DecodeImage(ReadOnlyMemory<byte> encoded, int width, int height, bool premultiply)
    {
        SKImageInfo imageInfo = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        SKAlphaType decodedAlphaType = premultiply ? SKAlphaType.Premul : SKAlphaType.Unpremul;
        using SKData pixels = SKData.Create(imageInfo.BytesSize64);

        DecodePixels(encoded, imageInfo.WithAlphaType(decodedAlphaType), pixels.Data);

        SKImage image = SKImage.FromPixels(imageInfo, pixels, imageInfo.RowBytes);
        SkiaMemoryPressure.TrackImage(image);

        return image;
    }

    private static unsafe void DecodePixels(ReadOnlyMemory<byte> encoded, SKImageInfo decodeInfo, IntPtr target)
    {
        fixed (byte* source = encoded.Span)
        {
            using SKData data = SKData.Create((IntPtr)source, encoded.Length);
            using SKCodec? codec = SKCodec.Create(data);

            if (codec is null)
            {
                throw new InvalidDataException("Invalid sprite data.");
            }

            SKCodecResult result = codec.GetPixels(decodeInfo, target);

            if (result != SKCodecResult.Success)
            {
                throw new InvalidDataException($"Cannot decode sprite data: {result}.");
            }
        }
    }
}
