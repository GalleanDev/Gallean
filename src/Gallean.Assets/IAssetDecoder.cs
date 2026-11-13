namespace Gallean.Assets;

public interface IAssetDecoder<TPixels>
{
    AssetSprite<TPixels> DecodeFrame(AssetSpriteMetadata metadata, ReadOnlyMemory<byte> data);

    AssetSpriteLayer<TPixels> DecodeLayer(AssetSpriteMetadata frame, int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> data);
}
