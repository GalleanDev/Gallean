namespace Gallean.Assets;

public sealed partial class AssetPack
{
    public sealed partial class Writer
    {
        public sealed record EncodedSpriteLayer(AssetSpriteLayerType Type, EncodedImage Image);
    }
}
