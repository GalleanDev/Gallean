namespace Packer;

internal abstract partial record PackImageEntry(string Id, IReadOnlyList<AssetTransform> Transforms) : PackEntry(Id)
{
    public abstract AssetKey Key { get; }
    public abstract IReadOnlyList<AssetSpriteMetadata> FrameMetadata { get; }
    public abstract IReadOnlyList<IReadOnlyList<AssetManifestSpriteLayer>> FrameLayers { get; }

    public abstract IReadOnlyList<AssetSprite> ReadFrames(AssetPack pack);

    public abstract IReadOnlyList<AssetPack.Writer.EncodedImage> ReadEncodedFrames(AssetPack pack);

    public abstract void Write(
        AssetPack.Writer writer,
        IReadOnlyList<AssetPack.Writer.EncodedFrame> frames,
        IReadOnlyList<AssetTransform> transforms);

    public static IEnumerable<PackImageEntry> Enumerate(AssetPack pack)
    {
        foreach (AssetManifestSprite sprite in pack.Manifest.Sprites)
        {
            yield return new SpriteEntry(sprite);
        }

        foreach (AssetManifestAnimation animation in pack.Manifest.Animations)
        {
            yield return new AnimationEntry(animation);
        }
    }

    public static IReadOnlyList<AssetPack.Writer.EncodedSpriteLayer> ReadSpriteLayers(
        AssetPack pack,
        IReadOnlyList<AssetManifestSpriteLayer> layers)
    {
        List<AssetPack.Writer.EncodedSpriteLayer> encoded = [];

        foreach (AssetManifestSpriteLayer layer in layers)
        {
            AssetPack.Writer.EncodedImage image = new(layer.PixelWidth, layer.PixelHeight, pack.ReadSpriteLayer(layer));

            encoded.Add(new AssetPack.Writer.EncodedSpriteLayer(layer.Type, image));
        }

        return encoded;
    }
}
