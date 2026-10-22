using ImageMagick;
using Umbra.Backgrounds;

namespace Packer;

internal static class BackgroundOverlayBuilder
{
    public static AssetAnimation? Build(
        AssetSpriteKey.BattleBackground background,
        int layer,
        UmbraBackgroundImage image,
        AssetSpriteMetadata metadata)
    {
        IReadOnlyList<UmbraFluidConfiguration> configurations = FluidMap.GetConfigurations(background);
        UmbraFluidConfiguration configuration = configurations[layer];

        if (layer >= image.Fluids.Count)
        {
            return null;
        }

        UmbraFluidMap map = DecodeFluidMap(image.Fluids[layer], $"{image.Name}:fluid{layer}");

        if (!map.Values.Span.ContainsAnyExcept(byte.MinValue))
        {
            return null;
        }

        UmbraFluidLoop loop = UmbraFluidBuilder.Build(configuration, map, metadata.Width, metadata.Height);

        List<AssetSprite> frames = [];

        foreach (byte[] data in loop.Frames)
        {
            ReadOnlyMemory<byte> framePixels = ToOpaquePixels(data);
            AssetSpriteMetadata frameMetadata = new(
                metadata.Width, metadata.Height, loop.Width, loop.Height, metadata.OffsetX, metadata.OffsetY, false);

            frames.Add(new AssetSprite(frameMetadata, framePixels));
        }

        AssetAnimationMetadata animationMetadata = new(
            frames.Count, metadata.Width, metadata.Height, metadata.OffsetX, metadata.OffsetY, null, null);

        return new AssetAnimation(animationMetadata, frames);

        static UmbraFluidMap DecodeFluidMap(UmbraBackgroundLayer layer, string name)
        {
            using MagickImage magick = new(layer.Data.Span);
            using IUnsafePixelCollection<byte> pixels = magick.GetPixelsUnsafe();
            byte[] values = pixels.ToByteArray("R") ?? throw new InvalidDataException($"Cannot read image '{name}'.");

            return new UmbraFluidMap(layer.PixelWidth, layer.PixelHeight, values);
        }

        static ReadOnlyMemory<byte> ToOpaquePixels(byte[] data)
        {
            byte[] result = new byte[data.Length * 4];

            for (int index = 0; index < data.Length; index++)
            {
                int offset = index * 4;

                result[offset] = data[index];
                result[offset + 1] = data[index];
                result[offset + 2] = data[index];
                result[offset + 3] = byte.MaxValue;
            }

            return result;
        }
    }
}
