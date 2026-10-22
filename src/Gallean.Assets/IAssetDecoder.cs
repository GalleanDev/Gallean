namespace Gallean.Assets;

public interface IAssetDecoder<out TPixels>
{
    TPixels DecodeFrame(AssetSpriteMetadata metadata, ReadOnlyMemory<byte> data);

    TPixels DecodeLayer(int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> data);
}
