using System.Buffers.Binary;
using Gallean.Common;
using Gallean.Engine.Contracts;
using Gallean.Tests.Common;

namespace Gallean.Assets.Tests;

public sealed class AssetPackTests
{
    private sealed class NullDecoder : IAssetDecoder<ReadOnlyMemory<byte>>, IStaticInstance<NullDecoder>
    {
        public ReadOnlyMemory<byte> DecodeFrame(AssetSpriteMetadata metadata, ReadOnlyMemory<byte> data)
        {
            return data;
        }

        public ReadOnlyMemory<byte> DecodeLayer(int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> data)
        {
            return data;
        }
    }

    private static string FormatSpriteMetadata(AssetSpriteMetadata metadata)
    {
        return $"{metadata.Width}x{metadata.Height} px {metadata.PixelWidth}x{metadata.PixelHeight} @{metadata.OffsetX},{metadata.OffsetY} {(metadata.Additive ? "additive" : "-")}";
    }

    private static string FormatSpriteLayer(AssetSpriteLayer layer)
    {
        string bytes = string.Join(",", [.. layer.Pixels.Span]);

        return $"px {layer.PixelWidth}x{layer.PixelHeight} [{bytes}]";
    }

    private static string FormatSprite(AssetSprite sprite)
    {
        string bytes = string.Join(",", [.. sprite.Pixels.Span]);

        return $"{FormatSpriteMetadata(sprite.Metadata)} [{bytes}]";
    }

    private static string FormatTransforms(IReadOnlyList<AssetTransform> transforms)
    {
        if (transforms.Count == 0)
        {
            return string.Empty;
        }

        JoinStringBuilder parts = new(",");

        foreach (AssetTransform transform in transforms)
        {
            parts.Add($"{transform.Type:N}@{transform.Scale}");
        }

        return $" transforms {parts}";
    }

    private static string FormatSpriteLayers(AssetPack pack, IReadOnlyList<AssetManifestSpriteLayer> layers)
    {
        if (layers.Count == 0)
        {
            return string.Empty;
        }

        JoinStringBuilder parts = new(" ");

        foreach (AssetManifestSpriteLayer layer in layers)
        {
            string bytes = string.Join(",", [.. pack.ReadSpriteLayer(layer).Span]);

            parts.Add($"{layer.Type} px {layer.PixelWidth}x{layer.PixelHeight} [{bytes}]");
        }

        return $" layers {parts}";
    }

    private static List<string> FormatPack(AssetPack pack)
    {
        List<string> rows = [];

        foreach (AssetManifestSprite sprite in pack.Manifest.Sprites)
        {
            string bytes = string.Join(",", [.. pack.ReadSprite(sprite).Span]);

            rows.Add(
                $"{sprite.Id} sprite {FormatSpriteMetadata(sprite.Metadata)} [{bytes}]" +
                $"{FormatTransforms(sprite.Transforms)}{FormatSpriteLayers(pack, sprite.Layers)}");
        }

        foreach (AssetManifestAnimation animation in pack.Manifest.Animations)
        {
            rows.Add($"{animation.Id} animation {animation.Metadata.FrameCount} {animation.Metadata.Width}x{animation.Metadata.Height} @{animation.Metadata.OffsetX},{animation.Metadata.OffsetY}{FormatTransforms(animation.Transforms)}");

            for (int index = 0; index < animation.Frames.Count; index++)
            {
                AssetManifestAnimationFrame frame = animation.Frames[index];
                string bytes = string.Join(",", [.. pack.ReadAnimationFrame(frame).Span]);

                rows.Add($"{animation.Id}#{index} {FormatSpriteMetadata(frame.Metadata)} [{bytes}]{FormatSpriteLayers(pack, frame.Layers)}");
            }
        }

        foreach (AssetManifestSound sound in pack.Manifest.Sounds)
        {
            string bytes = string.Join(",", ReadAll(pack.OpenSound(sound)));

            rows.Add($"{sound.Id} sound [{bytes}]");
        }

        return rows;
    }

    private static byte[] ReadAll(AssetStream stream)
    {
        using Stream data = stream.Open();
        using MemoryStream buffer = new();
        data.CopyTo(buffer);

        return buffer.ToArray();
    }

    private static AssetPack.Writer.EncodedImage MakeImage(int pixelWidth, int pixelHeight, byte[] data)
    {
        return new AssetPack.Writer.EncodedImage(pixelWidth, pixelHeight, data);
    }

    private static AssetPack.Writer.EncodedFrame MakeFrame(AssetSpriteMetadata metadata, AssetPack.Writer.EncodedImage image)
    {
        return new AssetPack.Writer.EncodedFrame(metadata, image, []);
    }

    private static AssetSound MakeSound(byte[] data)
    {
        return new AssetSound(new AssetSoundMetadata(), AssetStream.FromBytes(data));
    }

    [Test]
    public void RoundTripTest()
    {
        using Sandbox sb = Sandbox.Create();

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddSprite(
                "ground/human/0/0",
                MakeFrame(
                    new AssetSpriteMetadata(10, 12, PixelWidth: 0, PixelHeight: 0, 5, 6, Additive: false),
                    MakeImage(10, 12, [1, 2, 3])),
                [new AssetTransform(new Guid("11111111-1111-1111-1111-111111111111"), 1)]);

            writer.AddAnimation("water/0/0", new AssetAnimationMetadata(2, 20, 24, -2, -3, null, null), [
                MakeFrame(
                    new AssetSpriteMetadata(8, 8, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false),
                    MakeImage(8, 8, [4, 5])),
                MakeFrame(
                    new AssetSpriteMetadata(8, 8, PixelWidth: 0, PixelHeight: 0, 1, 2, Additive: true),
                    MakeImage(32, 32, [6, 7, 8, 9]))
            ], [
                new AssetTransform(new Guid("22222222-2222-2222-2222-222222222222"), 1),
                new AssetTransform(new Guid("33333333-3333-3333-3333-333333333333"), 4)
            ]);

            writer.AddSound("battle-unit-attack/x/0", MakeSound([7, 7]));

            writer.Complete();
        }

        using AssetPack pack = AssetPack.Read(sb.GetPath("test.galleanpack"));

        Assert.That(pack.Manifest.CreatedAt, Is.EqualTo(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)));
        Assert.That(pack.Manifest.CreatedBy, Is.EqualTo("0.1.0+2d40300"));
        Assert.That(pack.Manifest.Sparse, Is.False);
        Assert.That(FormatPack(pack), Is.EqualTo([
            "ground/human/0/0 sprite 10x12 px 10x12 @5,6 - [1,2,3] transforms 11111111111111111111111111111111@1",
            "water/0/0 animation 2 20x24 @-2,-3 transforms 22222222222222222222222222222222@1,33333333333333333333333333333333@4",
            "water/0/0#0 8x8 px 8x8 @0,0 - [4,5]",
            "water/0/0#1 8x8 px 32x32 @1,2 additive [6,7,8,9]",
            "battle-unit-attack/x/0 sound [7,7]"
        ]));

        AssetManifestSound sound = pack.Manifest.Sounds[0];
        Assert.That(ReadAll(pack.OpenSound(sound)), Is.EqualTo(new byte[] { 7, 7 }));
    }

    [Test]
    public void LayerRoundTripTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSpriteMetadata spriteMetadata = new(8, 8, PixelWidth: 0, PixelHeight: 0, 3, 4, Additive: false);
        AssetSpriteMetadata frameMetadata = new(6, 6, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false);

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddSprite(
                "unit-face/x",
                new AssetPack.Writer.EncodedFrame(spriteMetadata, MakeImage(16, 16, [1, 2]), [
                    new AssetPack.Writer.EncodedSpriteLayer(AssetSpriteLayerType.Normal, MakeImage(8, 8, [3, 4]))
                ]),
                []);

            writer.AddAnimation(
                "battle-unit/x/idle/attacker",
                new AssetAnimationMetadata(1, 6, 6, 0, 0, null, null),
                [
                    new AssetPack.Writer.EncodedFrame(frameMetadata, MakeImage(6, 6, [6, 7]), [
                        new AssetPack.Writer.EncodedSpriteLayer(AssetSpriteLayerType.Normal, MakeImage(6, 6, [8]))
                    ])
                ],
                []);

            writer.Complete();
        }

        using AssetPack pack = AssetPack.Read(sb.GetPath("test.galleanpack"));

        Assert.That(FormatPack(pack), Is.EqualTo([
            "unit-face/x sprite 8x8 px 16x16 @3,4 - [1,2] layers Normal px 8x8 [3,4]",
            "battle-unit/x/idle/attacker animation 1 6x6 @0,0",
            "battle-unit/x/idle/attacker#0 6x6 px 6x6 @0,0 - [6,7] layers Normal px 6x6 [8]"
        ]));
        Assert.That(File.ReadAllText(sb.GetPath("test.galleanpack")), Does.Contain("\"Type\":\"Normal\""));
    }

    [Test]
    public void LayerByKeyTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSpriteKey spriteKey = new AssetSpriteKey.Ground(WorldMapTerrain.Human);
        AssetSpriteKey plainSpriteKey = new AssetSpriteKey.Ground(WorldMapTerrain.Elf);
        AssetAnimationKey animationKey = new AssetAnimationKey.Water();
        AssetAnimationKey partialAnimationKey = new AssetAnimationKey.MountainRemoveEffect(1);
        AssetSpriteMetadata metadata = new(4, 4, PixelWidth: 0, PixelHeight: 0, 1, 2, Additive: false);

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddSprite(
                spriteKey.CanonicalId,
                new AssetPack.Writer.EncodedFrame(metadata, MakeImage(8, 8, [1]), [
                    new AssetPack.Writer.EncodedSpriteLayer(AssetSpriteLayerType.Normal, MakeImage(4, 4, [2, 3]))
                ]),
                []);

            writer.AddSprite(plainSpriteKey.CanonicalId, new AssetPack.Writer.EncodedFrame(metadata, MakeImage(8, 8, [9]), []), []);

            writer.AddAnimation(
                animationKey.CanonicalId,
                new AssetAnimationMetadata(2, 4, 4, 0, 0, null, null),
                [
                    new AssetPack.Writer.EncodedFrame(metadata, MakeImage(4, 4, [4]), [
                        new AssetPack.Writer.EncodedSpriteLayer(AssetSpriteLayerType.Normal, MakeImage(2, 2, [5]))
                    ]),
                    new AssetPack.Writer.EncodedFrame(metadata, MakeImage(4, 4, [7]), [
                        new AssetPack.Writer.EncodedSpriteLayer(AssetSpriteLayerType.Normal, MakeImage(2, 2, [8]))
                    ])
                ],
                []);

            writer.AddAnimation(
                partialAnimationKey.CanonicalId,
                new AssetAnimationMetadata(2, 4, 4, 0, 0, null, null),
                [
                    new AssetPack.Writer.EncodedFrame(metadata, MakeImage(4, 4, [10]), [
                        new AssetPack.Writer.EncodedSpriteLayer(AssetSpriteLayerType.Normal, MakeImage(2, 2, [11]))
                    ]),
                    new AssetPack.Writer.EncodedFrame(metadata, MakeImage(4, 4, [12]), [])
                ],
                []);

            writer.Complete();
        }

        using AssetPack pack = AssetPack.Read(sb.GetPath("test.galleanpack"));
        AssetPackSource<ReadOnlyMemory<byte>> source = new([pack], NullDecoder.Instance);

        AssetSpriteLayer normal = Assert.NotNull(source.TryGetSpriteLayer(spriteKey, AssetSpriteLayerType.Normal));

        Assert.That(FormatSpriteLayer(normal), Is.EqualTo("px 4x4 [2,3]"));
        Assert.That(source.TryGetSpriteLayer(plainSpriteKey, AssetSpriteLayerType.Normal), Is.Null);

        IReadOnlyList<AssetSpriteLayer> frames = Assert.NotNull(
            source.TryGetAnimationLayer(animationKey, AssetSpriteLayerType.Normal));

        List<string> rows = [];

        foreach (AssetSpriteLayer frame in frames)
        {
            rows.Add(FormatSpriteLayer(frame));
        }

        Assert.That(rows, Is.EqualTo(["px 2x2 [5]", "px 2x2 [8]"]));
        Assert.That(source.TryGetAnimationLayer(partialAnimationKey, AssetSpriteLayerType.Normal), Is.Null);
    }

    [Test]
    public void DuplicateLayerTest()
    {
        using Sandbox sb = Sandbox.Create();

        using AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");

        AssetSpriteMetadata metadata = new(4, 4, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false);

        Assert.That(
            () => writer.AddSprite(
                "unit-face/x",
                new AssetPack.Writer.EncodedFrame(metadata, MakeImage(4, 4, [1]), [
                    new AssetPack.Writer.EncodedSpriteLayer(AssetSpriteLayerType.Normal, MakeImage(4, 4, [2])),
                    new AssetPack.Writer.EncodedSpriteLayer(AssetSpriteLayerType.Normal, MakeImage(4, 4, [3]))
                ]),
                []),
            Throws.ArgumentException);
    }

    [Test]
    public void NonUniformLayerSizeTest()
    {
        using Sandbox sb = Sandbox.Create();

        using AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");

        AssetSpriteMetadata metadata = new(8, 4, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false);

        Assert.That(
            () => writer.AddSprite(
                "unit-face/x",
                new AssetPack.Writer.EncodedFrame(metadata, MakeImage(8, 4, [1]), [
                    new AssetPack.Writer.EncodedSpriteLayer(AssetSpriteLayerType.Normal, MakeImage(8, 16, [2]))
                ]),
                []),
            Throws.ArgumentException);
    }

    [Test]
    public void TriggerFrameTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetAnimationMetadata metadata = new(FrameCount: 46, Width: 108, Height: 314, OffsetX: -52, OffsetY: -284, 39, null);

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddAnimation(
                "world-spell-effect/g000ss0008/under/0",
                metadata with { FrameCount = 1 },
                [MakeFrame(new AssetSpriteMetadata(8, 8, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(8, 8, [1, 2]))],
                []);

            writer.Complete();
        }

        using AssetPack pack = AssetPack.Read(sb.GetPath("test.galleanpack"));

        Assert.That(pack.Manifest.Animations[0].Metadata, Is.EqualTo(metadata with { FrameCount = 1 }));
    }

    [Test]
    public void DeduplicateFramesTest()
    {
        using Sandbox sb = Sandbox.Create();

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddAnimation("battle-unit/x/idle/attacker", new AssetAnimationMetadata(1, 4, 4, 0, 0, null, null), [MakeFrame(new AssetSpriteMetadata(4, 4, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(4, 4, [9, 9, 9]))], []);
            writer.AddAnimation("battle-unit/x/attack/attacker", new AssetAnimationMetadata(1, 4, 4, 0, 0, null, null), [MakeFrame(new AssetSpriteMetadata(4, 4, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(4, 4, [9, 9, 9]))], []);
            writer.AddSprite("unit-face/x", MakeFrame(new AssetSpriteMetadata(4, 4, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(4, 4, [9, 9, 9])), []);

            writer.Complete();
        }

        using AssetPack pack = AssetPack.Read(sb.GetPath("test.galleanpack"));
        AssetManifestAnimationFrame first = pack.Manifest.Animations[0].Frames[0];
        AssetManifestAnimationFrame second = pack.Manifest.Animations[1].Frames[0];
        AssetManifestSprite sprite = pack.Manifest.Sprites[0];

        Assert.That(second.Offset, Is.EqualTo(first.Offset));
        Assert.That(sprite.Offset, Is.EqualTo(first.Offset));
        Assert.That(pack.ReadAnimationFrame(second).ToArray(), Is.EqualTo(new byte[] { 9, 9, 9 }));
        Assert.That(pack.ReadSprite(sprite).ToArray(), Is.EqualTo(new byte[] { 9, 9, 9 }));
    }

    [Test]
    public void DeduplicateSoundsTest()
    {
        using Sandbox sb = Sandbox.Create();

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddAnimation("battle-unit/x/idle/attacker", new AssetAnimationMetadata(1, 4, 4, 0, 0, null, null), [MakeFrame(new AssetSpriteMetadata(4, 4, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(4, 4, [9, 9, 9]))], []);

            writer.AddSound("battle-unit-attack/x/0", MakeSound([8, 8, 8]));
            writer.AddSound("battle-unit-attack/x/1", MakeSound([8, 8, 8]));

            writer.Complete();
        }

        using AssetPack pack = AssetPack.Read(sb.GetPath("test.galleanpack"));
        AssetManifestSound first = pack.Manifest.Sounds[0];
        AssetManifestSound second = pack.Manifest.Sounds[1];

        Assert.That(pack.Manifest.Sounds, Is.EqualTo([
            first with { Id = "battle-unit-attack/x/0", Metadata = new AssetSoundMetadata() },
            first with { Id = "battle-unit-attack/x/1", Metadata = new AssetSoundMetadata() }
        ]));
        Assert.That(second.Offset, Is.EqualTo(first.Offset));
        Assert.That(ReadAll(pack.OpenSound(second)), Is.EqualTo(new byte[] { 8, 8, 8 }));
    }

    [Test]
    public void EmptyAssetsTest()
    {
        using Sandbox sb = Sandbox.Create();

        using AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");

        AssetPack.Writer.EncodedFrame frame = MakeFrame(
            new AssetSpriteMetadata(4, 4, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(4, 4, [1]));

        Assert.That(() => writer.AddAnimation("water/0/0", new AssetAnimationMetadata(0, 4, 4, 0, 0, null, null), [], []), Throws.ArgumentException);
        Assert.That(() => writer.AddAnimation("water/0/0", new AssetAnimationMetadata(2, 4, 4, 0, 0, null, null), [frame], []), Throws.ArgumentException);
    }

    [Test]
    public void NonUniformPixelSizeTest()
    {
        using Sandbox sb = Sandbox.Create();

        using AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");

        Assert.That(
            () => writer.AddSprite("unit-face/x", MakeFrame(new AssetSpriteMetadata(8, 4, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(16, 12, [1])), []),
            Throws.ArgumentException);
        Assert.That(
            () => writer.AddAnimation(
                "water/0/0",
                new AssetAnimationMetadata(1, 8, 4, 0, 0, null, null),
                [MakeFrame(new AssetSpriteMetadata(8, 4, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(8, 16, [1]))],
                []),
            Throws.ArgumentException);
    }

    [Test]
    public void MissingAssetsTest()
    {
        using Sandbox sb = Sandbox.Create();

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.Complete();
        }

        using AssetPack pack = AssetPack.Read(sb.GetPath("test.galleanpack"));
        AssetPackSource<ReadOnlyMemory<byte>> source = new([pack], NullDecoder.Instance);
        AssetSpriteKey spriteKey = new AssetSpriteKey.Ground(WorldMapTerrain.Human);
        AssetAnimationKey animationKey = new AssetAnimationKey.Water();
        AssetSoundKey soundKey = new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("g000uu0001"), Variant: 0);

        Assert.That(source.TryGetSprite(spriteKey), Is.Null);
        Assert.That(source.TryGetSpriteMetadata(spriteKey), Is.Null);
        Assert.That(source.TryGetAnimation(animationKey), Is.Null);
        Assert.That(source.TryGetAnimationMetadata(animationKey), Is.Null);
        Assert.That(source.TryGetSound(soundKey), Is.Null);
        Assert.That(source.TryGetStream(spriteKey), Is.Null);
        Assert.That(source.TryGetStream(animationKey), Is.Null);
        Assert.That(source.TryGetStream(soundKey), Is.Null);
    }

    [Test]
    public void StreamByKeyTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSpriteKey spriteKey = new AssetSpriteKey.Ground(WorldMapTerrain.Human);
        AssetAnimationKey animationKey = new AssetAnimationKey.Water();
        AssetSoundKey soundKey = new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("g000uu0001"), Variant: 0);

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddSprite(
                spriteKey.CanonicalId,
                MakeFrame(new AssetSpriteMetadata(10, 12, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(10, 12, [1, 2, 3])),
                []);

            writer.AddAnimation(
                animationKey.CanonicalId,
                new AssetAnimationMetadata(1, 8, 8, 0, 0, null, null),
                [MakeFrame(new AssetSpriteMetadata(8, 8, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(8, 8, [4, 5]))],
                []);

            writer.AddSound(soundKey.CanonicalId, MakeSound([7, 7]));

            writer.Complete();
        }

        using AssetPack pack = AssetPack.Read(sb.GetPath("test.galleanpack"));
        AssetPackSource<ReadOnlyMemory<byte>> source = new([pack], NullDecoder.Instance);

        Assert.That(ReadAll(Assert.NotNull(source.TryGetStream(spriteKey))), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(ReadAll(Assert.NotNull(source.TryGetStream(soundKey))), Is.EqualTo(new byte[] { 7, 7 }));
        Assert.That(source.TryGetStream(animationKey), Is.Null);
    }

    [Test]
    public void SoundStreamTest()
    {
        using Sandbox sb = Sandbox.Create();

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("test.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddSound("battle-unit-attack/x/0", MakeSound([1, 2, 3, 4]));
            writer.AddSound("battle-unit-attack/x/1", MakeSound([5, 6]));

            writer.Complete();
        }

        using AssetPack pack = AssetPack.Read(sb.GetPath("test.galleanpack"));
        AssetStream sound = pack.OpenSound(pack.Manifest.Sounds[0]);

        using Stream data = sound.Open();
        byte[] buffer = new byte[8];

        Assert.That(data.Length, Is.EqualTo(4));
        Assert.That(data.Read(buffer), Is.EqualTo(4));
        Assert.That(buffer, Is.EqualTo(new byte[] { 1, 2, 3, 4, 0, 0, 0, 0 }));
        Assert.That(data.Read(buffer), Is.Zero);
        Assert.That(data.Position, Is.EqualTo(4));

        data.Seek(-3, SeekOrigin.End);

        Assert.That(data.Position, Is.EqualTo(1));
        Assert.That(() => data.Seek(5, SeekOrigin.Begin), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(ReadAll(sound), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        Assert.That(ReadAll(pack.OpenSound(pack.Manifest.Sounds[1])), Is.EqualTo(new byte[] { 5, 6 }));
    }

    [Test]
    public void NoStagingFilesTest()
    {
        using Sandbox sb = Sandbox.Create();

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("done.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddSprite("unit-face/x", MakeFrame(new AssetSpriteMetadata(4, 4, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(4, 4, [1, 2, 3])), []);

            Assert.That(sb.GetTree(), Is.EquivalentTo(["done.galleanpack.tmp"]));

            writer.Complete();
        }

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("abandoned.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddSprite("unit-face/x", MakeFrame(new AssetSpriteMetadata(4, 4, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(4, 4, [1, 2, 3])), []);
        }

        Assert.That(sb.GetTree(), Is.EquivalentTo(["done.galleanpack"]));
    }

    [Test]
    public void InvalidManifestRangeTest()
    {
        using Sandbox sb = Sandbox.Create();

        Assert.That(() => AssetPack.Read(WritePack(sb, "negative-length.galleanpack", 32, -1)), Throws.InstanceOf<InvalidDataException>());
        Assert.That(() => AssetPack.Read(WritePack(sb, "truncated.galleanpack", 32, 64)), Throws.InstanceOf<InvalidDataException>());
        Assert.That(() => AssetPack.Read(WritePack(sb, "huge-length.galleanpack", 32, long.MaxValue)), Throws.InstanceOf<InvalidDataException>());
        Assert.That(() => AssetPack.Read(WritePack(sb, "overlapping.galleanpack", 0, 0)), Throws.InstanceOf<InvalidDataException>());
        Assert.That(() => AssetPack.Read(WritePack(sb, "negative-offset.galleanpack", -1, 0)), Throws.InstanceOf<InvalidDataException>());
        Assert.That(() => AssetPack.Read(WritePack(sb, "past-end.galleanpack", 1024, 0)), Throws.InstanceOf<InvalidDataException>());
        Assert.That(() => AssetPack.Read(WritePack(sb, "huge-offset.galleanpack", long.MaxValue, 0)), Throws.InstanceOf<InvalidDataException>());

        static string WritePack(Sandbox sb, string name, long manifestOffset, long manifestLength)
        {
            byte[] header = new byte[32];
            "GALLEANPACK"u8.CopyTo(header);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(12), AssetPack.CurrentFormat);
            BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(16), manifestOffset);
            BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(24), manifestLength);

            string path = sb.GetPath(name);
            File.WriteAllBytes(path, header);

            return path;
        }
    }

    [Test]
    public void InvalidDataTest()
    {
        using Sandbox sb = Sandbox.Create();

        string path = sb.GetPath("foreign.bin");
        File.WriteAllBytes(path, [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]);

        Assert.That(() => AssetPack.Read(path), Throws.InstanceOf<InvalidDataException>());
    }

    [Test]
    public void UnsupportedVersionTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] header = new byte[32];
        "GALLEANPACK"u8.CopyTo(header);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(12), AssetPack.CurrentFormat - 1);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(16), 32);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(24), 0);

        string path = sb.GetPath("old.galleanpack");
        File.WriteAllBytes(path, header);

        Assert.Throws<AssetPackUnsupportedFormatException>(() => AssetPack.Read(path));
    }

    [Test]
    public void ManifestMaxSizeTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] header = new byte[32];
        "GALLEANPACK"u8.CopyTo(header);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(12), AssetPack.CurrentFormat);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(16), 32);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(24), 268435457);

        string path = sb.GetPath("oversized.galleanpack");

        using (FileStream stream = File.Create(path))
        {
            stream.Write(header);
            stream.SetLength(268435489);
        }

        Assert.That(() => AssetPack.Read(path), Throws.InstanceOf<InvalidDataException>());
    }

    [Test]
    public void OverlayTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSpriteKey replacedKey = new AssetSpriteKey.Ground(WorldMapTerrain.Human);
        AssetSpriteKey keptKey = new AssetSpriteKey.Ground(WorldMapTerrain.Elf);
        AssetAnimationKey addedKey = new AssetAnimationKey.Water();
        AssetSoundKey soundKey = new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("g000uu0001"), Variant: 0);
        AssetSpriteMetadata metadata = new(10, 12, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false);

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("base.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddSprite(
                replacedKey.CanonicalId,
                new AssetPack.Writer.EncodedFrame(metadata, MakeImage(10, 12, [1]), [
                    new AssetPack.Writer.EncodedSpriteLayer(AssetSpriteLayerType.Normal, MakeImage(10, 12, [2]))
                ]),
                []);

            writer.AddSprite(keptKey.CanonicalId, MakeFrame(metadata, MakeImage(10, 12, [3])), []);
            writer.AddSound(soundKey.CanonicalId, MakeSound([7, 7]));

            writer.Complete();
        }

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("overlay.galleanpack"), sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddSprite(replacedKey.CanonicalId, MakeFrame(metadata, MakeImage(20, 24, [4])), []);

            writer.AddAnimation(
                addedKey.CanonicalId,
                new AssetAnimationMetadata(1, 10, 12, 0, 0, null, null),
                [MakeFrame(metadata, MakeImage(10, 12, [5]))],
                []);

            writer.Complete();
        }

        using AssetPackSource<ReadOnlyMemory<byte>> source = new(
            [AssetPack.Read(sb.GetPath("base.galleanpack")), AssetPack.Read(sb.GetPath("overlay.galleanpack"))],
            NullDecoder.Instance);

        AssetSprite replaced = Assert.NotNull(source.TryGetSprite(replacedKey));
        AssetSprite kept = Assert.NotNull(source.TryGetSprite(keptKey));
        AssetAnimation added = Assert.NotNull(source.TryGetAnimation(addedKey));

        List<string> frames = [];

        foreach (AssetSprite frame in added.Frames)
        {
            frames.Add(FormatSprite(frame));
        }

        Assert.That(FormatSprite(replaced), Is.EqualTo("10x12 px 20x24 @0,0 - [4]"));
        Assert.That(source.TryGetSpriteLayer(replacedKey, AssetSpriteLayerType.Normal), Is.Null);
        Assert.That(FormatSprite(kept), Is.EqualTo("10x12 px 10x12 @0,0 - [3]"));
        Assert.That(frames, Is.EqualTo(["10x12 px 10x12 @0,0 - [5]"]));
        Assert.That(ReadAll(Assert.NotNull(source.TryGetStream(soundKey))), Is.EqualTo(new byte[] { 7, 7 }));
    }

    [Test]
    public void SparsePackTest()
    {
        using Sandbox sb = Sandbox.Create();

        using (AssetPack.Writer writer = AssetPack.Writer.Create(sb.GetPath("sparse.galleanpack"), sparse: true, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300"))
        {
            writer.AddSprite(
                "ground/human/0/0",
                MakeFrame(new AssetSpriteMetadata(10, 12, PixelWidth: 0, PixelHeight: 0, 0, 0, Additive: false), MakeImage(10, 12, [1])),
                []);

            writer.Complete();
        }

        using AssetPack pack = AssetPack.Read(sb.GetPath("sparse.galleanpack"));

        Assert.That(pack.Manifest.Sparse, Is.True);
        Assert.That(FormatPack(pack), Is.EqualTo(["ground/human/0/0 sprite 10x12 px 10x12 @0,0 - [1]"]));
    }
}
