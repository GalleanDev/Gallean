namespace Gallean.Assets;

public sealed partial class AssetPack
{
    public sealed partial class Writer
    {
        public sealed record EncodedFrame(
            AssetSpriteMetadata Metadata,
            EncodedImage Image,
            IReadOnlyList<EncodedSpriteLayer> Layers);
    }
}
