using Gallean.Common;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class AssetProvider
{
    private sealed class ImageDecoder : IAssetDecoder<SKImage>, IStaticInstance<ImageDecoder>
    {
        public SKImage DecodeFrame(AssetSpriteMetadata metadata, ReadOnlyMemory<byte> data)
        {
            return DecodeImage(data, metadata.PixelWidth, metadata.PixelHeight, premultiply: !metadata.Additive);
        }

        public SKImage DecodeLayer(int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> data)
        {
            return DecodeImage(data, pixelWidth, pixelHeight, premultiply: true);
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
}
