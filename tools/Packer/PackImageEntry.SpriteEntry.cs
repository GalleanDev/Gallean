using ImageMagick;

namespace Packer;

internal abstract partial record PackImageEntry
{
    private sealed record SpriteEntry(AssetManifestSprite Manifest)
        : PackImageEntry(Manifest.Id, Manifest.Transforms)
    {
        public override AssetKey Key { get; } = AssetSpriteKey.Parse(Manifest.Id, null);

        public override IReadOnlyList<AssetSpriteMetadata> FrameMetadata => [Manifest.Metadata];
        public override IReadOnlyList<IReadOnlyList<AssetManifestSpriteLayer>> FrameLayers => [Manifest.Layers];

        public override IReadOnlyList<AssetSprite> ReadFrames(AssetPack pack)
        {
            byte[] pixels = ImageDecoder.DecodePixels(pack.ReadSprite(Manifest), AlphaAction.Set, PixelMapping.RGBA);

            return [new AssetSprite(Manifest.Metadata, pixels)];
        }

        public override IReadOnlyList<AssetPack.Writer.EncodedImage> ReadEncodedFrames(AssetPack pack)
        {
            return
            [
                new AssetPack.Writer.EncodedImage(
                    Manifest.Metadata.PixelWidth, Manifest.Metadata.PixelHeight, pack.ReadSprite(Manifest))
            ];
        }

        public override void CopyTo(AssetPack pack, AssetPack.Writer writer)
        {
            AssetPack.Writer.EncodedImage image = new(
                Manifest.Metadata.PixelWidth, Manifest.Metadata.PixelHeight, pack.ReadSprite(Manifest));

            writer.AddSprite(
                Id,
                new AssetPack.Writer.EncodedFrame(Manifest.Metadata, image, ReadSpriteLayers(pack, Manifest.Layers)),
                Transforms);
        }

        public override void Write(
            AssetPack.Writer writer,
            IReadOnlyList<AssetPack.Writer.EncodedFrame> frames,
            IReadOnlyList<AssetTransform> transforms)
        {
            writer.AddSprite(Id, frames[0], transforms);
        }
    }
}
