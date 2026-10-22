using System.Numerics;
using ImageMagick;
using Umbra.Backgrounds;

namespace Packer;

internal static class BackgroundBuilder
{
    public const int BattleSceneWidth = 950;
    public const int MainMenuSceneWidth = 800;

    private const int SceneHeight = 600;

    public static AssetPack.Writer.EncodedFrame Build(UmbraBackgroundImage image, int sceneWidth)
    {
        return new AssetPack.Writer.EncodedFrame(
            GetMetadata(image, sceneWidth),
            MakeImage(image.Color),
            [
                MakeLayer(image.Normal, AssetSpriteLayerType.Normal),
                MakeLayer(image.Depth, AssetSpriteLayerType.Depth),
                MakeLayer(image.Albedo, AssetSpriteLayerType.Albedo)
            ]);

        static AssetPack.Writer.EncodedImage MakeImage(UmbraBackgroundLayer layer)
        {
            return new AssetPack.Writer.EncodedImage(layer.PixelWidth, layer.PixelHeight, layer.Data);
        }

        static AssetPack.Writer.EncodedSpriteLayer MakeLayer(UmbraBackgroundLayer layer, AssetSpriteLayerType type)
        {
            byte[] pixels = ImageDecoder.DecodePixels(layer.Data, AlphaAction.Set, PixelMapping.RGBA);
            AssetSpriteLayer decoded = new(layer.PixelWidth, layer.PixelHeight, pixels);
            AssetPack.Writer.EncodedImage encoded = FrameEncoder.EncodeBackgroundLayer(decoded, type);

            return new AssetPack.Writer.EncodedSpriteLayer(type, encoded);
        }
    }

    public static AssetSpriteMetadata GetMetadata(UmbraBackgroundImage image, int sceneWidth)
    {
        UmbraBackgroundLayer color = image.Color;
        int divisor = (int)BigInteger.GreatestCommonDivisor(color.PixelWidth, color.PixelHeight);
        int aspectWidth = color.PixelWidth / divisor;
        int aspectHeight = color.PixelHeight / divisor;
        int aspectScale = Math.Max(1, (int)Math.Round(SceneHeight / (double)aspectHeight));
        int width = aspectWidth * aspectScale;
        int height = aspectHeight * aspectScale;
        int offsetX = (sceneWidth / 2) - (width / 2);

        return new AssetSpriteMetadata(width, height, color.PixelWidth, color.PixelHeight, offsetX, 0, Additive: false);
    }
}
