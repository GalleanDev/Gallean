using Gallean.Common;
using Gallean.Engine.Contracts;
using Gallean.Tests.Common;
using GameContent;
using ImageMagick;
using NAudio.Wave;
using NVorbis;
using Umbra.Backgrounds;

namespace Packer.Tests;

public sealed class PackBuilderTests
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

    private sealed class MagickDecoder : IAssetDecoder<ReadOnlyMemory<byte>>, IStaticInstance<MagickDecoder>
    {
        public ReadOnlyMemory<byte> DecodeFrame(AssetSpriteMetadata metadata, ReadOnlyMemory<byte> data)
        {
            return DecodePixels(data);
        }

        public ReadOnlyMemory<byte> DecodeLayer(int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> data)
        {
            return DecodePixels(data);
        }
    }

    private sealed class TestReader(AssetSprite? sprite, AssetAnimation? animation, AssetSound? sound) : IGameAssetReader
    {
        public AssetSprite? Read(GameAssetDescriptor.Sprite descriptor)
        {
            return sprite;
        }

        public AssetAnimation? Read(GameAssetDescriptor.Animation descriptor)
        {
            return animation;
        }

        public AssetSound? Read(GameAssetDescriptor.Sound descriptor)
        {
            return sound;
        }
    }

    private static byte[] DecodePixels(ReadOnlyMemory<byte> data)
    {
        using MagickImage image = new(data.Span);
        image.Alpha(AlphaAction.Set);

        using IUnsafePixelCollection<byte> collection = image.GetPixelsUnsafe();

        return collection.ToByteArray(PixelMapping.RGBA) ?? [];
    }

    private static IReadOnlyList<PackBuilderEntry> SelectEntries(GameAssetInputs inputs, IReadOnlyList<AssetKey> keys)
    {
        IReadOnlyList<GameAssetDescriptor> descriptors = GameAssetCatalog.ListDescriptors(inputs);
        IReadOnlyList<PackBuilderEntry> entries = PackBuilderCatalog.ListEntries(descriptors);

        return [.. entries.Where(entry => keys.Contains(entry.Key))];
    }

    private static IReadOnlyList<string> FormatLayers(AssetPackSource<ReadOnlyMemory<byte>> source, AssetSpriteKey key)
    {
        List<string> rows = [];

        foreach (AssetSpriteLayerType type in Enum.GetValues<AssetSpriteLayerType>())
        {
            AssetSpriteLayer? layer = source.TryGetSpriteLayer(key, type);

            if (layer is not null)
            {
                rows.Add($"{type} {layer.PixelWidth}x{layer.PixelHeight}");
            }
        }

        return rows;
    }

    private static List<byte[]> GetFramePixels(AssetAnimation animation)
    {
        List<byte[]> frames = [];

        foreach (AssetSprite frame in animation.Frames)
        {
            frames.Add(frame.Pixels.ToArray());
        }

        return frames;
    }

    private static string FormatMetadata(AssetSpriteMetadata metadata)
    {
        string additive = metadata.Additive ? " additive" : string.Empty;

        return $"{metadata.Width}x{metadata.Height} px {metadata.PixelWidth}x{metadata.PixelHeight} at {metadata.OffsetX},{metadata.OffsetY}{additive}";
    }

    private static string FormatMetadata(AssetAnimationMetadata metadata)
    {
        string box = $"{metadata.FrameCount} frames {metadata.Width}x{metadata.Height} at {metadata.OffsetX},{metadata.OffsetY}";

        if (metadata.TriggerFrame is null)
        {
            return box;
        }
        else
        {
            return $"{box} trigger {metadata.TriggerFrame}";
        }
    }

    private static string FormatTransforms(IReadOnlyList<AssetTransform> transforms)
    {
        JoinStringBuilder parts = new(",");

        foreach (AssetTransform transform in transforms)
        {
            parts.Add($"{transform.Type:N}@{transform.Scale}");
        }

        return $"[{parts}]";
    }

    private static List<string> FormatPack(AssetPack pack)
    {
        List<string> rows = [];

        foreach (AssetManifestSprite sprite in pack.Manifest.Sprites)
        {
            rows.Add($"{sprite.Id} sprite {FormatMetadata(sprite.Metadata)} {FormatTransforms(sprite.Transforms)}");
        }

        foreach (AssetManifestAnimation animation in pack.Manifest.Animations)
        {
            rows.Add($"{animation.Id} animation {FormatMetadata(animation.Metadata)} {FormatTransforms(animation.Transforms)}");

            for (int index = 0; index < animation.Frames.Count; index++)
            {
                AssetManifestAnimationFrame frame = animation.Frames[index];

                rows.Add($"{animation.Id}#{index} {FormatMetadata(frame.Metadata)}");
            }
        }

        foreach (AssetManifestSound sound in pack.Manifest.Sounds)
        {
            rows.Add($"{sound.Id} sound");
        }

        return rows;
    }

    [Test]
    public void PackSoundsTest()
    {
        using Sandbox sb = Sandbox.Create();

        MemoryStream wave = new();

        using (WaveFileWriter writer = new(wave, new WaveFormat(44100, 16, 1)))
        {
            writer.WriteSamples(new short[4410], 0, 4410);
        }

        AssetSoundMetadata metadata = new(AnimationFrame: 3, StartTime: TimeSpan.FromMilliseconds(20.0));
        AssetSound sound = new(metadata, AssetStream.FromBytes(wave.ToArray()));
        UnitDefinitionId unitDefinitionId = new("g000uu0001");
        AssetSoundKey key = new AssetSoundKey.BattleUnitAttack(unitDefinitionId, Variant: 0);
        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilder builder = new(
            SelectEntries(new GameAssetInputs([unitDefinitionId], [], [], [], [], [], []), [key]),
            new TestReader(null, null, sound),
            _ => { });

        builder.Build(options);

        using AssetPack pack = AssetPack.Read(options.OutputPath);
        AssetPackSource<ReadOnlyMemory<byte>> source = new([pack], NullDecoder.Instance);

        AssetSound loaded = Assert.NotNull(source.TryGetSound(key));

        using Stream data = loaded.Data.Open();
        using VorbisReader reader = new(data, closeOnDispose: false);

        Assert.That(loaded.Metadata, Is.EqualTo(new AssetSoundMetadata(AnimationFrame: 3, StartTime: TimeSpan.FromMilliseconds(20.0))));
        Assert.That(reader.Channels, Is.EqualTo(2));
        Assert.That(reader.SampleRate, Is.EqualTo(44100));
        Assert.That(reader.TotalSamples, Is.EqualTo(4410));
    }

    [Test]
    public void MissingEntriesTest()
    {
        using Sandbox sb = Sandbox.Create();

        UnitDefinitionId unitDefinitionId = new("g000uu0001");
        AssetSpriteKey.UnitFace face = new(unitDefinitionId);
        AssetSpriteKey.BattleCorpse corpse = new(IsLarge: false, Variant: 0);
        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilder builder = new(
            SelectEntries(new GameAssetInputs([unitDefinitionId], [], [], [], [], [], []), [face, corpse]),
            new TestReader(null, null, null),
            _ => { });

        PackBuilderResult.MissingRequired result = Assert.IsOfType<PackBuilderResult.MissingRequired>(builder.Build(options));

        Assert.That(result.AssetIds, Is.EqualTo(["battle-corpse/small/0", "unit-face/g000uu0001"]));
        Assert.That(File.Exists(options.OutputPath), Is.False);
    }

    [Test]
    public void PositionedAnimationBoxTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetAnimationMetadata box = new(1, 800, 600, 0, 0, TriggerFrame: null, SecondTriggerFrame: null);
        byte[] pixels =
        [
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 255, 255
        ];
        AssetSprite frame = new(new AssetSpriteMetadata(2, 2, 2, 2, 40, 30, Additive: false), pixels);

        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilder builder = new(
            SelectEntries(
                new GameAssetInputs([], [], [], [], [], [], []),
                [new AssetAnimationKey.Ruin(IsLooted: false, Variant: 0)]),
            new TestReader(null, new AssetAnimation(box, [frame]), null),
            _ => { });

        builder.Build(options);

        using AssetPack pack = AssetPack.Read(options.OutputPath);

        Assert.That(FormatPack(pack), Is.EqualTo([
            "ruin/intact/0 animation 1 frames 800x600 at 0,0 []",
            "ruin/intact/0#0 2x2 px 2x2 at 40,30"
        ]));
    }

    [Test]
    public void ExistingOutputTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] pixels =
        [
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 255, 255
        ];
        AssetSprite sprite = new(new AssetSpriteMetadata(2, 2, 2, 2, 0, 0, Additive: false), pixels);

        UnitDefinitionId unitDefinitionId = new("g000uu0001");
        AssetSpriteKey.UnitFace key = new(unitDefinitionId);
        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilder builder = new(
            SelectEntries(new GameAssetInputs([unitDefinitionId], [], [], [], [], [], []), [key]),
            new TestReader(sprite, null, null),
            _ => { });

        PackBuilderResult first = builder.Build(options);
        PackBuilderResult second = builder.Build(options);

        Assert.That(first, Is.EqualTo(new PackBuilderResult.Packed(options.OutputPath)));
        Assert.That(second, Is.EqualTo(new PackBuilderResult.Packed(options.OutputPath)));
    }

    [Test]
    public void DeterministicPackTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] pixels =
        [
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 255, 255
        ];
        AssetSprite sprite = new(new AssetSpriteMetadata(2, 2, 2, 2, 0, 0, Additive: false), pixels);

        List<PackBuilderEntry> entries =
        [
            .. GameAssetCatalog.ListDescriptors(new GameAssetInputs([], [], [], [], [], [], []))
                .Where(entry => entry is GameAssetDescriptor.Sprite)
                .Take(32)
                .Select(descriptor => new PackBuilderEntry.GameAsset(descriptor)),
            new PackBuilderEntry.Background(new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.City(), Variant: 0), BackgroundBuilder.BattleSceneWidth)
        ];
        PackBuilder builder = new(entries, new TestReader(sprite, null, null), _ => { });
        PackBuilderOptions first = new(sb.GetPath("first.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilderOptions second = new(sb.GetPath("second.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");

        builder.Build(first);
        builder.Build(second);

        Assert.That(File.ReadAllBytes(second.OutputPath), Is.EqualTo(File.ReadAllBytes(first.OutputPath)));
    }

    [Test]
    public void WaterLandmarkTintTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] pixels =
        [
            40, 110, 108, 255,
            240, 244, 244, 255,
            122, 90, 60, 255,
            40, 110, 108, 255
        ];
        AssetSprite frame = new(new AssetSpriteMetadata(2, 2, 2, 2, 0, 0, Additive: false), pixels);
        AssetAnimation animation = new(new AssetAnimationMetadata(1, 2, 2, 0, 0, TriggerFrame: null, SecondTriggerFrame: null), [frame]);

        WorldLandmarkDefinitionId landmarkId = new("g000mg0004");
        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        AssetAnimationKey plainKey = new AssetAnimationKey.Landmark(landmarkId);
        AssetAnimationKey tintedKey = new AssetAnimationKey.WaterLandmark(landmarkId);
        PackBuilder builder = new(
            SelectEntries(new GameAssetInputs([], [], [], [], [landmarkId], [], []), [plainKey, tintedKey]),
            new TestReader(null, animation, null),
            _ => { });

        builder.Build(options);

        using AssetPack pack = AssetPack.Read(options.OutputPath);
        AssetPackSource<ReadOnlyMemory<byte>> source = new([pack], MagickDecoder.Instance);
        AssetAnimation plain = Assert.NotNull(source.TryGetAnimation(plainKey));
        AssetAnimation tinted = Assert.NotNull(source.TryGetAnimation(tintedKey));

        byte[] expectedPlain =
        [
            40, 110, 108, 255,
            240, 244, 244, 255,
            122, 90, 60, 255,
            40, 110, 108, 255
        ];

        byte[] expectedTinted =
        [
            44, 128, 139, 255,
            241, 246, 247, 255,
            122, 90, 60, 255,
            44, 128, 139, 255
        ];

        Assert.That(FormatPack(pack), Is.EqualTo([
            "landmark/g000mg0004 animation 1 frames 2x2 at 0,0 []",
            "landmark/g000mg0004#0 2x2 px 2x2 at 0,0",
            "water-landmark/g000mg0004 animation 1 frames 2x2 at 0,0 [e46d70f814844e078e6b94ba9597ada2@1]",
            "water-landmark/g000mg0004#0 2x2 px 2x2 at 0,0"
        ]));

        Assert.That(GetFramePixels(plain), Is.EqualTo([expectedPlain]));
        Assert.That(GetFramePixels(tinted), Is.EqualTo([expectedTinted]));
    }

    [Test]
    public void ShadowTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] pixels =
        [
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 255, 255
        ];
        AssetSprite sprite = new(new AssetSpriteMetadata(2, 2, 2, 2, 5, 7, Additive: false), pixels);

        AssetAnimation animation = new(new AssetAnimationMetadata(1, 2, 2, 5, 7, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);

        UnitDefinitionId unitDefinitionId = new("g000uu0001");
        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilder builder = new(
            SelectEntries(
                new GameAssetInputs([unitDefinitionId], [], [], [], [], [], []),
                [
                    new AssetAnimationKey.StackUnit(unitDefinitionId, AssetAnimationKey.StackUnitPose.Stop, Direction: 0),
                    new AssetAnimationKey.StackUnitShadow(unitDefinitionId, AssetAnimationKey.StackUnitPose.Stop, Direction: 0)
                ]),
            new TestReader(null, animation, null),
            _ => { });

        builder.Build(options);

        using AssetPack pack = AssetPack.Read(options.OutputPath);

        Assert.That(FormatPack(pack), Is.EqualTo([
            "stack-unit/g000uu0001/stop/0 animation 1 frames 2x2 at 5,7 []",
            "stack-unit/g000uu0001/stop/0#0 2x2 px 2x2 at 5,7",
            "stack-unit-shadow/g000uu0001/stop/0 animation 1 frames 8x8 at 2,4 [b3471958d5db4d7ab82f400eb5bd8e8b@1]",
            "stack-unit-shadow/g000uu0001/stop/0#0 8x8 px 8x8 at 2,4"
        ]));
    }

    [Test]
    public void AdditiveTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] pixels = [10, 20, 30, 40, 50, 60, 70, 255];
        AssetSprite sprite = new(new AssetSpriteMetadata(2, 1, 2, 1, 5, 6, Additive: true), pixels);
        AssetAnimation animation = new(new AssetAnimationMetadata(1, 2, 1, 5, 6, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);

        UnitDefinitionId unitDefinitionId = new("g000uu0001");
        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilder builder = new(
            SelectEntries(
                new GameAssetInputs([unitDefinitionId], [], [], [], [], [], []),
                [new AssetAnimationKey.BattleUnitAura(unitDefinitionId, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker)]),
            new TestReader(null, animation, null),
            _ => { });

        builder.Build(options);

        using AssetPack pack = AssetPack.Read(options.OutputPath);

        Assert.That(FormatPack(pack), Is.EqualTo([
            "battle-unit-aura/g000uu0001/idle/attacker animation 1 frames 18x17 at -3,-2 [73de3ef1c6ca4304be0946bbc27d5446@1,268df81e71734aac8653a10cb1e295a5@1]",
            "battle-unit-aura/g000uu0001/idle/attacker#0 18x17 px 18x17 at -3,-2 additive"
        ]));
    }

    [Test]
    public void TrimmedAdditiveBoundsTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] pixels =
        [
            10, 20, 30, 200,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            1, 1, 1, 3
        ];
        AssetSprite sprite = new(new AssetSpriteMetadata(12, 1, 12, 1, 5, 6, Additive: true), pixels);
        AssetAnimation animation = new(new AssetAnimationMetadata(1, 12, 1, 5, 6, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);

        UnitDefinitionId unitDefinitionId = new("g000uu0001");
        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilder builder = new(
            SelectEntries(
                new GameAssetInputs([unitDefinitionId], [], [], [], [], [], []),
                [new AssetAnimationKey.StackUnitAura(unitDefinitionId, AssetAnimationKey.StackUnitPose.Stop, Direction: 0)]),
            new TestReader(null, animation, null),
            _ => { });

        builder.Build(options);

        using AssetPack pack = AssetPack.Read(options.OutputPath);

        Assert.That(FormatPack(pack), Is.EqualTo([
            "stack-unit-aura/g000uu0001/stop/0 animation 1 frames 17x17 at -3,-2 [73de3ef1c6ca4304be0946bbc27d5446@1,268df81e71734aac8653a10cb1e295a5@1]",
            "stack-unit-aura/g000uu0001/stop/0#0 17x17 px 17x17 at -3,-2 additive"
        ]));
    }

    [Test]
    public void BackgroundOverlayTest()
    {
        AssetSpriteKey.BattleBackground key = new(new AssetSpriteKey.BattleLocation.Ruin(), Variant: 1);
        UmbraBackgroundImage image = Assert.NotNull(UmbraBackgroundLibrary.TryGet(key.CanonicalId));
        AssetSpriteMetadata metadata = new(24, 10, 24, 10, -5, 0, Additive: false);

        AssetAnimation overlay = Assert.NotNull(BackgroundOverlayBuilder.Build(key, 0, image, metadata));

        Assert.That(overlay.Metadata, Is.EqualTo(new AssetAnimationMetadata(72, 24, 10, -5, 0, TriggerFrame: null, SecondTriggerFrame: null)));
        Assert.That(overlay.Frames[0].Metadata, Is.EqualTo(new AssetSpriteMetadata(24, 10, 24, 10, -5, 0, Additive: false)));
    }

    [Test]
    public void BackgroundLayersTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSpriteKey.BattleBackground key = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), Variant: 2);
        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilder builder = new(
            SelectEntries(new GameAssetInputs([], [], [], [], [], [], []), [key]),
            new TestReader(null, null, null),
            _ => { });

        builder.Build(options);

        using AssetPack pack = AssetPack.Read(options.OutputPath);
        AssetPackSource<ReadOnlyMemory<byte>> source = new([pack], NullDecoder.Instance);

        AssetSprite sprite = Assert.NotNull(source.TryGetSprite(key));

        Assert.That(sprite.Metadata, Is.EqualTo(new AssetSpriteMetadata(1440, 600, 2880, 1200, -245, 0, Additive: false)));

        Assert.That(FormatLayers(source, key), Is.EqualTo(["Normal 5760x2400", "Depth 1920x800", "Albedo 5760x2400"]));
    }

    [Test]
    public void EmptyFluidMapTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSpriteKey.BattleBackground key = new(new AssetSpriteKey.BattleLocation.City(), Variant: 0);
        AssetAnimationKey.BattleBackgroundOverlay overlayKey = new(key.Location, key.Variant, 0);
        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilder builder = new(
            SelectEntries(new GameAssetInputs([], [], [], [], [], [], []), [key, overlayKey]),
            new TestReader(null, null, null),
            _ => { });

        builder.Build(options);

        using AssetPack pack = AssetPack.Read(options.OutputPath);
        AssetPackSource<ReadOnlyMemory<byte>> source = new([pack], NullDecoder.Instance);

        Assert.That(source.TryGetAnimation(overlayKey), Is.Null);
    }

    [Test]
    public void CatalogTest()
    {
        IReadOnlyList<GameAssetDescriptor> descriptors = GameAssetCatalog.ListDescriptors(
            new GameAssetInputs([new UnitDefinitionId("g000uu0001")], [], [], [], [], [], []));
        SortedDictionary<string, int> counts = [];

        foreach (PackBuilderEntry entry in PackBuilderCatalog.ListEntries(descriptors))
        {
            string row = $"{entry.GetType().Name} {(entry.Required ? "required" : "optional")}";

            counts[row] = counts.GetValueOrDefault(row) + 1;
        }

        List<string> rows = [];

        foreach ((string row, int count) in counts)
        {
            rows.Add($"{row} {count}");
        }

        Assert.That(rows, Is.EqualTo([
            "Background required 32",
            "BackgroundOverlay optional 36",
            "GameAsset optional 447",
            "GameAsset required 858"
        ]));
    }

    [Test]
    public void LosslessPackTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] pixels =
        [
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 255, 255
        ];
        AssetSprite frame = new(new AssetSpriteMetadata(2, 2, 2, 2, 0, 0, Additive: false), pixels);
        AssetAnimation animation = new(new AssetAnimationMetadata(1, 2, 2, 0, 0, TriggerFrame: null, SecondTriggerFrame: null), [frame]);

        UnitDefinitionId unitDefinitionId = new("g000uu0001");
        AssetAnimationKey key = new AssetAnimationKey.BattleUnit(unitDefinitionId, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker);
        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilder builder = new(
            SelectEntries(new GameAssetInputs([unitDefinitionId], [], [], [], [], [], []), [key]),
            new TestReader(null, animation, null),
            _ => { });

        builder.Build(options);

        using AssetPack pack = AssetPack.Read(options.OutputPath);
        AssetPackSource<ReadOnlyMemory<byte>> source = new([pack], MagickDecoder.Instance);
        AssetAnimation loaded = Assert.NotNull(source.TryGetAnimation(key));

        byte[] expected =
        [
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 255, 255
        ];

        Assert.That(FormatPack(pack), Is.EqualTo([
            "battle-unit/g000uu0001/idle/attacker animation 1 frames 2x2 at 0,0 []",
            "battle-unit/g000uu0001/idle/attacker#0 2x2 px 2x2 at 0,0"
        ]));

        Assert.That(GetFramePixels(loaded), Is.EqualTo([expected]));
    }

    [Test]
    public void ManifestTest()
    {
        using Sandbox sb = Sandbox.Create();

        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilder builder = new(
            SelectEntries(new GameAssetInputs([], [], [], [], [], [], []), []),
            new TestReader(null, null, null),
            _ => { });

        builder.Build(options);

        using AssetPack pack = AssetPack.Read(options.OutputPath);

        Assert.That(pack.Manifest.CreatedAt, Is.EqualTo(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)));
        Assert.That(pack.Manifest.CreatedBy, Is.EqualTo("0.1.0+2d40300"));
        Assert.That(pack.Manifest.Sparse, Is.False);
    }

    [Test]
    public void MainMenuBackgroundV2Test()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSpriteKey.MainMenuBackgroundV2 key = new();
        PackBuilderOptions options = new(sb.GetPath("test.galleanpack"), new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        PackBuilder builder = new(
            SelectEntries(new GameAssetInputs([], [], [], [], [], [], []), [key]),
            new TestReader(null, null, null),
            _ => { });

        builder.Build(options);

        using AssetPack pack = AssetPack.Read(options.OutputPath);
        AssetPackSource<ReadOnlyMemory<byte>> source = new([pack], NullDecoder.Instance);

        Assert.That(FormatPack(pack), Is.EqualTo(["main-menu-background-v2 sprite 800x600 px 3200x2400 at 0,0 []"]));

        Assert.That(FormatLayers(source, key), Is.EqualTo(["Normal 3200x2400", "Depth 1536x1152", "Albedo 3200x2400"]));
    }

    [Test]
    public void SoundRecordTest()
    {
        IReadOnlyList<GameAssetDescriptor> descriptors = GameAssetCatalog.ListDescriptors(
            new GameAssetInputs([new UnitDefinitionId("g000uu8008"), new UnitDefinitionId("g000uu0157")], [], [], [], [], [], []));
        List<string> rows = [];

        foreach (PackBuilderEntry entry in PackBuilderCatalog.ListEntries(descriptors))
        {
            switch (entry)
            {
                case PackBuilderEntry.UnitSound sound:
                    rows.Add($"{sound.Key.CanonicalId} sound of {sound.RecordDescriptor.Key.CanonicalId}");
                    break;

                case PackBuilderEntry.UnitAttack attack:
                    rows.Add($"{attack.Key.CanonicalId} triggers of {attack.RecordDescriptor.Key.CanonicalId}");
                    break;
            }
        }

        Assert.That(rows, Is.EqualTo([
            "battle-unit/g000uu8008/attack/attacker triggers of battle-unit/g000uu0157/attack/attacker",
            "battle-unit/g000uu8008/attack/defender triggers of battle-unit/g000uu0157/attack/defender",
            "battle-effect-attack/g000uu8008/hit/body/attack/0 triggers of battle-effect-attack/g000uu0157/hit/body/attack/0",
            "battle-effect-attack/g000uu8008/hit/body/attack/1 triggers of battle-effect-attack/g000uu0157/hit/body/attack/1",
            "battle-effect-attack/g000uu8008/hit/body/attack/2 triggers of battle-effect-attack/g000uu0157/hit/body/attack/2",
            "battle-effect-attack/g000uu8008/hit/body/defend/0 triggers of battle-effect-attack/g000uu0157/hit/body/defend/0",
            "battle-effect-attack/g000uu8008/hit/body/defend/1 triggers of battle-effect-attack/g000uu0157/hit/body/defend/1",
            "battle-effect-attack/g000uu8008/hit/body/defend/2 triggers of battle-effect-attack/g000uu0157/hit/body/defend/2",
            "battle-effect-attack/g000uu8008/hit/body/both/0 triggers of battle-effect-attack/g000uu0157/hit/body/both/0",
            "battle-effect-attack/g000uu8008/hit/body/both/1 triggers of battle-effect-attack/g000uu0157/hit/body/both/1",
            "battle-effect-attack/g000uu8008/hit/body/both/2 triggers of battle-effect-attack/g000uu0157/hit/body/both/2",
            "battle-effect-attack/g000uu8008/touch/body/attack/0 triggers of battle-effect-attack/g000uu0157/touch/body/attack/0",
            "battle-effect-attack/g000uu8008/touch/body/attack/1 triggers of battle-effect-attack/g000uu0157/touch/body/attack/1",
            "battle-effect-attack/g000uu8008/touch/body/attack/2 triggers of battle-effect-attack/g000uu0157/touch/body/attack/2",
            "battle-effect-attack/g000uu8008/touch/body/defend/0 triggers of battle-effect-attack/g000uu0157/touch/body/defend/0",
            "battle-effect-attack/g000uu8008/touch/body/defend/1 triggers of battle-effect-attack/g000uu0157/touch/body/defend/1",
            "battle-effect-attack/g000uu8008/touch/body/defend/2 triggers of battle-effect-attack/g000uu0157/touch/body/defend/2",
            "battle-effect-attack/g000uu8008/touch/body/both/0 triggers of battle-effect-attack/g000uu0157/touch/body/both/0",
            "battle-effect-attack/g000uu8008/touch/body/both/1 triggers of battle-effect-attack/g000uu0157/touch/body/both/1",
            "battle-effect-attack/g000uu8008/touch/body/both/2 triggers of battle-effect-attack/g000uu0157/touch/body/both/2",
            "battle-unit-attack/g000uu8008/0 sound of battle-unit-attack/g000uu0157/0",
            "battle-unit-attack/g000uu8008/1 sound of battle-unit-attack/g000uu0157/1",
            "battle-unit-attack/g000uu8008/2 sound of battle-unit-attack/g000uu0157/2",
            "battle-unit-attack/g000uu8008/3 sound of battle-unit-attack/g000uu0157/3",
            "battle-unit-attack/g000uu8008/4 sound of battle-unit-attack/g000uu0157/4",
            "battle-unit-attack/g000uu8008/5 sound of battle-unit-attack/g000uu0157/5",
            "battle-unit-hit/g000uu8008/0 sound of battle-unit-hit/g000uu0157/0",
            "battle-unit-hit/g000uu8008/1 sound of battle-unit-hit/g000uu0157/1",
            "battle-unit-hit/g000uu8008/2 sound of battle-unit-hit/g000uu0157/2",
            "battle-unit-hit/g000uu8008/3 sound of battle-unit-hit/g000uu0157/3",
            "battle-unit-hit/g000uu8008/4 sound of battle-unit-hit/g000uu0157/4",
            "battle-unit-touch/g000uu8008/0 sound of battle-unit-touch/g000uu0157/0",
            "battle-unit-touch/g000uu8008/1 sound of battle-unit-touch/g000uu0157/1",
            "battle-unit-touch/g000uu8008/2 sound of battle-unit-touch/g000uu0157/2",
            "battle-unit-touch/g000uu8008/3 sound of battle-unit-touch/g000uu0157/3",
            "battle-unit-touch/g000uu8008/4 sound of battle-unit-touch/g000uu0157/4",
            "battle-unit-damaged/g000uu8008/0 sound of battle-unit-damaged/g000uu0157/0",
            "battle-unit-damaged/g000uu8008/1 sound of battle-unit-damaged/g000uu0157/1",
            "battle-unit-damaged/g000uu8008/2 sound of battle-unit-damaged/g000uu0157/2",
            "battle-unit-damaged/g000uu8008/3 sound of battle-unit-damaged/g000uu0157/3",
            "stack-unit-move/g000uu8008/0 sound of stack-unit-move/g000uu0157/0",
            "stack-unit-move/g000uu8008/1 sound of stack-unit-move/g000uu0157/1",
            "stack-unit-move/g000uu8008/2 sound of stack-unit-move/g000uu0157/2",
            "stack-unit-move/g000uu8008/3 sound of stack-unit-move/g000uu0157/3"
        ]));
    }
}
