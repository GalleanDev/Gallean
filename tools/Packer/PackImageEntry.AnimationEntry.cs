using ImageMagick;

namespace Packer;

internal abstract partial record PackImageEntry
{
    private sealed record AnimationEntry(AssetManifestAnimation Manifest)
        : PackImageEntry(Manifest.Id, Manifest.Transforms)
    {
        public override AssetKey Key { get; } = AssetAnimationKey.Parse(Manifest.Id, null);

        public override IReadOnlyList<AssetSpriteMetadata> FrameMetadata
        {
            get
            {
                List<AssetSpriteMetadata> metadata = [];

                foreach (AssetManifestAnimationFrame frame in Manifest.Frames)
                {
                    metadata.Add(frame.Metadata);
                }

                return metadata;
            }
        }

        public override IReadOnlyList<IReadOnlyList<AssetManifestSpriteLayer>> FrameLayers
        {
            get
            {
                List<IReadOnlyList<AssetManifestSpriteLayer>> layers = [];

                foreach (AssetManifestAnimationFrame frame in Manifest.Frames)
                {
                    layers.Add(frame.Layers);
                }

                return layers;
            }
        }

        public override IReadOnlyList<AssetSprite> ReadFrames(AssetPack pack)
        {
            List<AssetSprite> frames = [];

            foreach (AssetManifestAnimationFrame frame in Manifest.Frames)
            {
                byte[] pixels = ImageDecoder.DecodePixels(pack.ReadAnimationFrame(frame), AlphaAction.Set, PixelMapping.RGBA);

                frames.Add(new AssetSprite(frame.Metadata, pixels));
            }

            return frames;
        }

        public override IReadOnlyList<AssetPack.Writer.EncodedImage> ReadEncodedFrames(AssetPack pack)
        {
            List<AssetPack.Writer.EncodedImage> images = [];

            foreach (AssetManifestAnimationFrame frame in Manifest.Frames)
            {
                images.Add(new AssetPack.Writer.EncodedImage(
                    frame.Metadata.PixelWidth, frame.Metadata.PixelHeight, pack.ReadAnimationFrame(frame)));
            }

            return images;
        }

        public override void CopyTo(AssetPack pack, AssetPack.Writer writer)
        {
            List<AssetPack.Writer.EncodedFrame> frames = [];

            foreach (AssetManifestAnimationFrame frame in Manifest.Frames)
            {
                AssetPack.Writer.EncodedImage image = new(
                    frame.Metadata.PixelWidth, frame.Metadata.PixelHeight, pack.ReadAnimationFrame(frame));

                frames.Add(new AssetPack.Writer.EncodedFrame(frame.Metadata, image, ReadSpriteLayers(pack, frame.Layers)));
            }

            writer.AddAnimation(Id, Manifest.Metadata, frames, Transforms);
        }

        public override void Write(
            AssetPack.Writer writer,
            IReadOnlyList<AssetPack.Writer.EncodedFrame> frames,
            IReadOnlyList<AssetTransform> transforms)
        {
            List<AssetSpriteMetadata> metadata = [];

            foreach (AssetPack.Writer.EncodedFrame frame in frames)
            {
                metadata.Add(frame.Metadata);
            }

            writer.AddAnimation(Id, FrameEncoder.ExpandBounds(Manifest.Metadata, metadata), frames, transforms);
        }
    }
}
