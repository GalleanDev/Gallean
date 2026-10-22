using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Gallean.Common;

namespace Gallean.Assets;

public sealed partial class AssetPack
{
    public sealed partial class Writer(AtomicFileWriter file, bool sparse, DateTimeOffset createdAt, string createdBy) : IDisposable
    {
        private readonly List<AssetManifestAnimation> animations = [];
        private readonly Dictionary<string, (long Offset, int Length)> deduplicated = [];
        private readonly List<AssetManifestSound> sounds = [];
        private readonly List<AssetManifestSprite> sprites = [];

        public void Dispose()
        {
            file.Dispose();
        }

        public static Writer Create(string path, bool sparse, DateTimeOffset createdAt, string createdBy)
        {
            string? directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            AtomicFileWriter file = new(path);
            file.Stream.Write(new byte[HeaderLength]);

            return new Writer(file, sparse, createdAt, createdBy);
        }

        public void AddSprite(string id, EncodedFrame frame, IReadOnlyList<AssetTransform> transforms)
        {
            (long Offset, int Length) location = WriteImage(id, frame.Metadata, frame.Image);

            AssetSpriteMetadata stored = frame.Metadata with
            {
                PixelWidth = frame.Image.PixelWidth,
                PixelHeight = frame.Image.PixelHeight
            };

            sprites.Add(new AssetManifestSprite(id, stored, location.Offset, location.Length, transforms, WriteSpriteLayers(id, frame)));
        }

        public void AddAnimation(
            string id,
            AssetAnimationMetadata animationMetadata,
            IReadOnlyList<EncodedFrame> frames,
            IReadOnlyList<AssetTransform> transforms)
        {
            if (frames.Count <= 0)
            {
                throw new ArgumentException("Animation must have at least one frame.", nameof(frames));
            }

            if (animationMetadata.FrameCount != frames.Count)
            {
                throw new ArgumentException("Animation metadata frame count must match frame count.", nameof(animationMetadata));
            }

            List<AssetManifestAnimationFrame> manifestFrames = [];

            foreach (EncodedFrame frame in frames)
            {
                (long Offset, int Length) location = WriteImage(id, frame.Metadata, frame.Image);

                AssetSpriteMetadata frameMetadata = frame.Metadata with
                {
                    PixelWidth = frame.Image.PixelWidth,
                    PixelHeight = frame.Image.PixelHeight
                };

                manifestFrames.Add(new AssetManifestAnimationFrame(frameMetadata, location.Offset, location.Length, WriteSpriteLayers(id, frame)));
            }

            animations.Add(new AssetManifestAnimation(id, animationMetadata, manifestFrames, transforms));
        }

        public void AddSound(string id, AssetSound sound)
        {
            using Stream data = sound.Data.Open();
            using MemoryStream buffer = new();
            data.CopyTo(buffer);

            (long Offset, int Length) location = WriteBlob(buffer.GetBuffer().AsMemory(0, (int)buffer.Length));

            sounds.Add(new AssetManifestSound(id, sound.Metadata, location.Offset, location.Length));
        }

        private IReadOnlyList<AssetManifestSpriteLayer> WriteSpriteLayers(string id, EncodedFrame frame)
        {
            List<AssetManifestSpriteLayer> layers = [];
            HashSet<AssetSpriteLayerType> written = [];

            foreach (EncodedSpriteLayer layer in frame.Layers)
            {
                if (!written.Add(layer.Type))
                {
                    throw new ArgumentException($"Duplicate '{layer.Type}' layer for asset '{id}'.");
                }

                (long Offset, int Length) location = WriteImage(id, frame.Metadata, layer.Image);

                layers.Add(new AssetManifestSpriteLayer(
                    layer.Type, layer.Image.PixelWidth, layer.Image.PixelHeight, location.Offset, location.Length));
            }

            return layers;
        }

        private (long Offset, int Length) WriteImage(string id, AssetSpriteMetadata metadata, EncodedImage image)
        {
            if ((long)image.PixelWidth * metadata.Height != (long)image.PixelHeight * metadata.Width)
            {
                throw new ArgumentException($"Pixel/logical size mismatch for asset '{id}'.");
            }

            return WriteBlob(image.Data);
        }

        private (long Offset, int Length) WriteBlob(ReadOnlyMemory<byte> data)
        {
            string key = Convert.ToHexStringLower(SHA256.HashData(data.Span));

            if (deduplicated.TryGetValue(key, out (long Offset, int Length) location))
            {
                return location;
            }

            long offset = file.Stream.Position - HeaderLength;
            file.Stream.Write(data.Span);
            deduplicated[key] = (offset, data.Length);

            return (offset, data.Length);
        }

        public void Complete()
        {
            AssetManifest manifest = new(createdAt, createdBy, sparse, sprites, animations, sounds);
            byte[] manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, AssetPackJsonContext.Default.AssetManifest);

            if (manifestBytes.Length > MaxManifestLength)
            {
                throw new InvalidOperationException($"The asset pack manifest length '{manifestBytes.Length}' exceeds {MaxManifestLength}.");
            }

            long manifestOffset = file.Stream.Position;
            file.Stream.Write(manifestBytes);

            byte[] header = new byte[HeaderLength];
            Magic.CopyTo(header, 0);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(12), CurrentFormat);
            BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(16), manifestOffset);
            BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(24), manifestBytes.Length);

            file.Stream.Position = 0;
            file.Stream.Write(header);
            file.Commit();
        }
    }
}
