using Gallean.Common;
using Gallean.Engine.Contracts;
using Gallean.Tests.Common;
using ImageMagick;

namespace Packer.Tests;

[Explicit]
public sealed class PackEnhancerTests
{
    private static AssetPack.Writer.EncodedImage EncodePng(int pixelWidth, int pixelHeight, byte[] pixels)
    {
        PixelReadSettings settings = new((uint)pixelWidth, (uint)pixelHeight, StorageType.Char, PixelMapping.RGBA);

        using MagickImage image = new(pixels, settings);
        image.Format = MagickFormat.Png32;

        return new AssetPack.Writer.EncodedImage(pixelWidth, pixelHeight, image.ToByteArray());
    }

    private static byte[] DecodePixels(ReadOnlyMemory<byte> data)
    {
        using MagickImage image = new(data.Span);
        image.Alpha(AlphaAction.Set);

        using IUnsafePixelCollection<byte> pixels = image.GetPixelsUnsafe();

        return pixels.ToByteArray(PixelMapping.RGBA) ?? [];
    }

    private static byte[] MakePixels(int pixelWidth, int pixelHeight, byte[] color)
    {
        byte[] pixels = new byte[pixelWidth * pixelHeight * 4];

        for (int index = 0; index < pixelWidth * pixelHeight; index++)
        {
            color.CopyTo(pixels, index * 4);
        }

        return pixels;
    }

    private static List<string> GetCachedFileNames(string cacheDirectory)
    {
        List<string> names = [];

        foreach (string path in Directory.EnumerateFiles(cacheDirectory))
        {
            names.Add(Path.GetFileName(path));
        }

        return names;
    }

    private static List<string> GetCachedLayerNames(string cacheDirectory)
    {
        List<string> names = [];

        foreach (string path in Directory.EnumerateFiles(cacheDirectory))
        {
            string name = Path.GetFileName(path);
            int deviceEnd = name.IndexOf('.');
            int hashEnd = name.IndexOf('.', deviceEnd + 1);

            names.Add(name[(hashEnd + 1)..]);
        }

        names.Sort(StringComparer.Ordinal);

        return names;
    }

    private static string WritePack(Sandbox sb)
    {
        string path = sb.GetPath("base.galleanpack");
        AssetSpriteMetadata metadata = new(2, 2, 2, 2, 0, 0, Additive: false);
        UnitDefinitionId unitDefinitionId = new("G000UU0001");

        AssetPack.Writer.EncodedImage first = EncodePng(2, 2, MakePixels(2, 2, [10, 20, 30, 255]));
        AssetPack.Writer.EncodedImage second = EncodePng(2, 2, MakePixels(2, 2, [40, 50, 60, 255]));
        AssetPack.Writer.EncodedImage shadowFirst = EncodePng(2, 2, MakePixels(2, 2, [70, 80, 90, 255]));
        AssetPack.Writer.EncodedImage shadowSecond = EncodePng(2, 2, MakePixels(2, 2, [100, 110, 120, 255]));

        using (AssetPack.Writer writer = AssetPack.Writer.Create(path, sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.0.9+1c2b3a4"))
        {
            writer.AddSprite(
                new AssetSpriteKey.UnitFace(unitDefinitionId).CanonicalId,
                new AssetPack.Writer.EncodedFrame(metadata, first, []),
                []);

            writer.AddAnimation(
                new AssetAnimationKey.BattleUnit(unitDefinitionId, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker).CanonicalId,
                new AssetAnimationMetadata(2, 2, 2, 0, 0, TriggerFrame: null, SecondTriggerFrame: null),
                [
                    new AssetPack.Writer.EncodedFrame(metadata, first, []),
                    new AssetPack.Writer.EncodedFrame(metadata, second, [])
                ],
                []);

            writer.AddAnimation(
                new AssetAnimationKey.BattleUnitShadow(unitDefinitionId, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker).CanonicalId,
                new AssetAnimationMetadata(2, 2, 2, 0, 0, TriggerFrame: null, SecondTriggerFrame: null),
                [
                    new AssetPack.Writer.EncodedFrame(metadata, shadowFirst, []),
                    new AssetPack.Writer.EncodedFrame(metadata, shadowSecond, [])
                ],
                []);

            writer.AddSound(
                new AssetSoundKey.BattleUnitAttack(unitDefinitionId, Variant: 0).CanonicalId,
                new AssetSound(new AssetSoundMetadata(), AssetStream.FromBytes(new byte[] { 1, 2, 3 })));

            writer.Complete();
        }

        return path;
    }

    private static List<string> FormatIds(AssetPack pack)
    {
        List<string> ids = [];

        foreach (AssetManifestSprite sprite in pack.Manifest.Sprites)
        {
            ids.Add(sprite.Id);
        }

        foreach (AssetManifestAnimation animation in pack.Manifest.Animations)
        {
            ids.Add(animation.Id);
        }

        foreach (AssetManifestSound sound in pack.Manifest.Sounds)
        {
            ids.Add(sound.Id);
        }

        return ids;
    }

    private static string FormatMetadata(AssetSpriteMetadata metadata)
    {
        return $"{metadata.Width}x{metadata.Height} px {metadata.PixelWidth}x{metadata.PixelHeight}";
    }

    private static string FormatTransforms(IReadOnlyList<AssetTransform> transforms)
    {
        JoinStringBuilder parts = new(",");

        foreach (AssetTransform transform in transforms)
        {
            parts.Add($"{transform.Type:N}@{transform.Scale}");
        }

        return $" [{parts}]";
    }

    private static string FormatLayers(IReadOnlyList<AssetManifestSpriteLayer> layers)
    {
        if (layers.Count == 0)
        {
            return string.Empty;
        }

        JoinStringBuilder parts = new(" ");

        foreach (AssetManifestSpriteLayer layer in layers)
        {
            parts.Add($"{layer.Type} {layer.PixelWidth}x{layer.PixelHeight}");
        }

        return $" layers {parts}";
    }

    private static List<string> FormatPack(AssetPack pack)
    {
        List<string> rows = [];

        foreach (AssetManifestSprite sprite in pack.Manifest.Sprites)
        {
            rows.Add(
                $"{sprite.Id} sprite {FormatMetadata(sprite.Metadata)}" +
                $"{FormatTransforms(sprite.Transforms)}{FormatLayers(sprite.Layers)}");
        }

        foreach (AssetManifestAnimation animation in pack.Manifest.Animations)
        {
            rows.Add($"{animation.Id} animation{FormatTransforms(animation.Transforms)}");

            for (int index = 0; index < animation.Frames.Count; index++)
            {
                AssetManifestAnimationFrame frame = animation.Frames[index];

                rows.Add($"{animation.Id}#{index} {FormatMetadata(frame.Metadata)}{FormatLayers(frame.Layers)}");
            }
        }

        return rows;
    }

    private static List<string> FormatColorBlobs(AssetPack pack)
    {
        List<string> rows = [];

        foreach (AssetManifestSprite sprite in pack.Manifest.Sprites)
        {
            rows.Add($"{sprite.Id} {Convert.ToHexStringLower(pack.ReadSprite(sprite).Span)}");
        }

        foreach (AssetManifestAnimation animation in pack.Manifest.Animations)
        {
            for (int index = 0; index < animation.Frames.Count; index++)
            {
                AssetManifestAnimationFrame frame = animation.Frames[index];

                rows.Add($"{animation.Id}#{index} {Convert.ToHexStringLower(pack.ReadAnimationFrame(frame).Span)}");
            }
        }

        return rows;
    }

    [Test]
    public void EnhanceTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestModels.TryGetUpscaleModelDirectory();
        TestModels.TryGetRefineModelDirectory();
        TestModels.TryGetAlphaModelDirectory();

        string modelDirectory = TestModels.TryGetSurfaceModelDirectory();

        using AssetPack pack = AssetPack.Read(WritePack(sb));

        PackEnhancerOptions options = new(
            Categories: null,
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: sb.GetPath("out.galleanpack"),
            Sparse: false,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: sb.GetPath("cache"),
            ScratchDirectory: sb.RootPath,
            [EnhancementType.Upscale, EnhancementType.Surface]);

        PackEnhancerResult result = new PackEnhancer(pack, _ => { }).Enhance(options);

        Assert.IsOfType<PackEnhancerResult.Enhanced>(result);

        using AssetPack enhanced = AssetPack.Read(sb.GetPath("out.galleanpack"));

        Assert.That(enhanced.Manifest.Sparse, Is.False);
        Assert.That(enhanced.Manifest.CreatedAt, Is.EqualTo(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)));
        Assert.That(enhanced.Manifest.CreatedBy, Is.EqualTo("0.1.0+2d40300"));

        Assert.That(FormatPack(enhanced), Is.EqualTo([
            "unit-face/g000uu0001 sprite 2x2 px 4x4 [b3d1f5a76c244e938f702a5c9de41b68@1]",
            "battle-unit/g000uu0001/idle/attacker animation [7638e1d713d74ed4abbbd514797df049@1,2ed23ad3f09c477fb3e30ce3bb53bbe6@2,9425579e408f470da0e3ba4a0375cff7@4,5d1e8a3792c44b6fa0e37c4f18b2d965@4]",
            "battle-unit/g000uu0001/idle/attacker#0 2x2 px 8x8 layers Normal 8x8",
            "battle-unit/g000uu0001/idle/attacker#1 2x2 px 8x8 layers Normal 8x8",
            "battle-unit-shadow/g000uu0001/idle/attacker animation [b3d1f5a76c244e938f702a5c9de41b68@1,b7c9f1a43d524e189f0c2a6b5d8e4713@2]",
            "battle-unit-shadow/g000uu0001/idle/attacker#0 8x8 px 16x16",
            "battle-unit-shadow/g000uu0001/idle/attacker#1 8x8 px 16x16"
        ]));

        Assert.That(FormatIds(enhanced), Is.EqualTo([
            "unit-face/g000uu0001",
            "battle-unit/g000uu0001/idle/attacker",
            "battle-unit-shadow/g000uu0001/idle/attacker",
            "battle-unit-attack/g000uu0001/0"
        ]));
    }

    [Test]
    public void RerunTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestModels.TryGetUpscaleModelDirectory();
        TestModels.TryGetRefineModelDirectory();
        TestModels.TryGetAlphaModelDirectory();

        string modelDirectory = TestModels.TryGetSurfaceModelDirectory();

        using AssetPack pack = AssetPack.Read(WritePack(sb));

        PackEnhancerOptions first = new(
            Categories: null,
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: sb.GetPath("first.galleanpack"),
            Sparse: false,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: sb.GetPath("cache"),
            ScratchDirectory: sb.RootPath,
            [EnhancementType.Upscale, EnhancementType.Surface]);

        new PackEnhancer(pack, _ => { }).Enhance(first);

        using AssetPack enhanced = AssetPack.Read(sb.GetPath("first.galleanpack"));

        PackEnhancerOptions again = new(
            Categories: null,
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: sb.GetPath("second.galleanpack"),
            Sparse: false,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: sb.GetPath("cache"),
            ScratchDirectory: sb.RootPath,
            [EnhancementType.Upscale, EnhancementType.Surface]);

        PackEnhancerResult result = new PackEnhancer(enhanced, _ => { }).Enhance(again);

        Assert.IsOfType<PackEnhancerResult.Enhanced>(result);

        using AssetPack second = AssetPack.Read(sb.GetPath("second.galleanpack"));

        Assert.That(FormatPack(second), Is.EqualTo([
            "unit-face/g000uu0001 sprite 2x2 px 8x8 [b3d1f5a76c244e938f702a5c9de41b68@1,b3d1f5a76c244e938f702a5c9de41b68@2]",
            "battle-unit/g000uu0001/idle/attacker animation [7638e1d713d74ed4abbbd514797df049@1,2ed23ad3f09c477fb3e30ce3bb53bbe6@2,9425579e408f470da0e3ba4a0375cff7@4,5d1e8a3792c44b6fa0e37c4f18b2d965@4,7638e1d713d74ed4abbbd514797df049@4,2ed23ad3f09c477fb3e30ce3bb53bbe6@8,9425579e408f470da0e3ba4a0375cff7@16]",
            "battle-unit/g000uu0001/idle/attacker#0 2x2 px 32x32 layers Normal 8x8",
            "battle-unit/g000uu0001/idle/attacker#1 2x2 px 32x32 layers Normal 8x8",
            "battle-unit-shadow/g000uu0001/idle/attacker animation [b3d1f5a76c244e938f702a5c9de41b68@1,b7c9f1a43d524e189f0c2a6b5d8e4713@2,b3d1f5a76c244e938f702a5c9de41b68@2]",
            "battle-unit-shadow/g000uu0001/idle/attacker#0 8x8 px 32x32",
            "battle-unit-shadow/g000uu0001/idle/attacker#1 8x8 px 32x32"
        ]));
    }

    [Test]
    public void CachedTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestModels.TryGetUpscaleModelDirectory();
        TestModels.TryGetRefineModelDirectory();
        TestModels.TryGetAlphaModelDirectory();

        string modelDirectory = TestModels.TryGetSurfaceModelDirectory();

        string packPath = WritePack(sb);
        string cacheDirectory = sb.GetPath("cache");

        using (AssetPack pack = AssetPack.Read(packPath))
        {
            PackEnhancerOptions computed = new(
                Categories: null,
                ModelDirectory: modelDirectory,
                SurfaceDenoisingSteps: 2,
                Device: UmbraDevice.Cpu,
                OutputPath: sb.GetPath("first.galleanpack"),
                Sparse: false,
                CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
                CreatedBy: "0.1.0+2d40300",
                CacheDirectory: cacheDirectory,
                ScratchDirectory: cacheDirectory,
                [EnhancementType.Upscale, EnhancementType.Surface]);

            new PackEnhancer(pack, _ => { }).Enhance(computed);
        }

        using AssetPack cached = AssetPack.Read(packPath);

        PackEnhancerOptions options = new(
            Categories: null,
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: sb.GetPath("second.galleanpack"),
            Sparse: false,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: cacheDirectory,
            ScratchDirectory: cacheDirectory,
            [EnhancementType.Upscale, EnhancementType.Surface]);

        PackEnhancerResult result = new PackEnhancer(cached, _ => { }).Enhance(options);

        Assert.IsOfType<PackEnhancerResult.Enhanced>(result);

        using AssetPack first = AssetPack.Read(sb.GetPath("first.galleanpack"));
        using AssetPack second = AssetPack.Read(sb.GetPath("second.galleanpack"));

        Assert.That(FormatPack(second), Is.EqualTo(FormatPack(first)));
        Assert.That(FormatColorBlobs(second), Is.EqualTo(FormatColorBlobs(first)));
    }

    [Test]
    public void CacheOnlyTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestModels.TryGetUpscaleModelDirectory();
        TestModels.TryGetRefineModelDirectory();
        TestModels.TryGetAlphaModelDirectory();

        string modelDirectory = TestModels.TryGetSurfaceModelDirectory();

        string packPath = WritePack(sb);
        string splitCacheDirectory = sb.GetPath("split-cache");
        string fullCacheDirectory = sb.GetPath("full-cache");

        using (AssetPack pack = AssetPack.Read(packPath))
        {
            PackEnhancerOptions computed = new(
                Categories: null,
                ModelDirectory: modelDirectory,
                SurfaceDenoisingSteps: 2,
                Device: UmbraDevice.Cpu,
                OutputPath: null,
                Sparse: false,
                CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
                CreatedBy: "0.1.0+2d40300",
                CacheDirectory: splitCacheDirectory,
                ScratchDirectory: splitCacheDirectory,
                [EnhancementType.Upscale, EnhancementType.Surface]);

            PackEnhancerResult result = new PackEnhancer(pack, _ => { }).Enhance(computed);

            Assert.That(result, Is.EqualTo(new PackEnhancerResult.Enhanced(
                OutputPath: null, EntryCount: 3, CacheEntryCount: 11)));
        }

        using (AssetPack pack = AssetPack.Read(packPath))
        {
            PackEnhancerOptions assembled = new(
                Categories: null,
                ModelDirectory: modelDirectory,
                SurfaceDenoisingSteps: 2,
                Device: UmbraDevice.Cpu,
                OutputPath: sb.GetPath("split.galleanpack"),
                Sparse: false,
                CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
                CreatedBy: "0.1.0+2d40300",
                CacheDirectory: splitCacheDirectory,
                ScratchDirectory: splitCacheDirectory,
                [EnhancementType.Upscale, EnhancementType.Surface]);

            new PackEnhancer(pack, _ => { }).Enhance(assembled);
        }

        using (AssetPack pack = AssetPack.Read(packPath))
        {
            PackEnhancerOptions full = new(
                Categories: null,
                ModelDirectory: modelDirectory,
                SurfaceDenoisingSteps: 2,
                Device: UmbraDevice.Cpu,
                OutputPath: sb.GetPath("full.galleanpack"),
                Sparse: false,
                CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
                CreatedBy: "0.1.0+2d40300",
                CacheDirectory: fullCacheDirectory,
                ScratchDirectory: fullCacheDirectory,
                [EnhancementType.Upscale, EnhancementType.Surface]);

            new PackEnhancer(pack, _ => { }).Enhance(full);
        }

        Assert.That(
            GetCachedFileNames(splitCacheDirectory),
            Is.EquivalentTo(GetCachedFileNames(fullCacheDirectory)));

        Assert.That(
            File.ReadAllBytes(sb.GetPath("split.galleanpack")),
            Is.EqualTo(File.ReadAllBytes(sb.GetPath("full.galleanpack"))));
    }

    [Test]
    public void EntryOrderTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = TestModels.TryGetUpscaleModelDirectory();

        string packPath = sb.GetPath("base.galleanpack");
        string cacheDirectory = sb.GetPath("cache");
        AssetSpriteMetadata metadata = new(2, 2, 2, 2, 0, 0, Additive: false);

        using (AssetPack.Writer writer = AssetPack.Writer.Create(packPath, sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.0.9+1c2b3a4"))
        {
            for (int index = 0; index < 8; index++)
            {
                UnitDefinitionId unitDefinitionId = new($"G000UU{index:D4}");

                AssetPack.Writer.EncodedImage sprite = EncodePng(
                    2, 2, MakePixels(2, 2, [(byte)(index * 2), 20, 30, 255]));

                AssetPack.Writer.EncodedImage frame = EncodePng(
                    2, 2, MakePixels(2, 2, [(byte)(index * 2 + 1), 20, 30, 255]));

                writer.AddSprite(
                    new AssetSpriteKey.UnitFace(unitDefinitionId).CanonicalId,
                    new AssetPack.Writer.EncodedFrame(metadata, sprite, []),
                    []);

                writer.AddAnimation(
                    new AssetAnimationKey.BattleUnitShadow(unitDefinitionId, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker).CanonicalId,
                    new AssetAnimationMetadata(1, 2, 2, 0, 0, TriggerFrame: null, SecondTriggerFrame: null),
                    [new AssetPack.Writer.EncodedFrame(metadata, frame, [])],
                    []);
            }

            writer.Complete();
        }

        using (AssetPack pack = AssetPack.Read(packPath))
        {
            PackEnhancerOptions options = new(
                Categories: null,
                ModelDirectory: modelDirectory,
                SurfaceDenoisingSteps: 2,
                Device: UmbraDevice.Cpu,
                OutputPath: sb.GetPath("first.galleanpack"),
                Sparse: false,
                CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
                CreatedBy: "0.1.0+2d40300",
                CacheDirectory: cacheDirectory,
                ScratchDirectory: sb.RootPath,
                [EnhancementType.Upscale]);

            new PackEnhancer(pack, _ => { }).Enhance(options);
        }

        using (AssetPack pack = AssetPack.Read(packPath))
        {
            PackEnhancerOptions options = new(
                Categories: null,
                ModelDirectory: modelDirectory,
                SurfaceDenoisingSteps: 2,
                Device: UmbraDevice.Cpu,
                OutputPath: sb.GetPath("second.galleanpack"),
                Sparse: false,
                CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
                CreatedBy: "0.1.0+2d40300",
                CacheDirectory: cacheDirectory,
                ScratchDirectory: sb.RootPath,
                [EnhancementType.Upscale]);

            new PackEnhancer(pack, _ => { }).Enhance(options);
        }

        using AssetPack enhanced = AssetPack.Read(sb.GetPath("second.galleanpack"));

        Assert.That(FormatIds(enhanced), Is.EqualTo([
            "unit-face/g000uu0000",
            "unit-face/g000uu0001",
            "unit-face/g000uu0002",
            "unit-face/g000uu0003",
            "unit-face/g000uu0004",
            "unit-face/g000uu0005",
            "unit-face/g000uu0006",
            "unit-face/g000uu0007",
            "battle-unit-shadow/g000uu0000/idle/attacker",
            "battle-unit-shadow/g000uu0001/idle/attacker",
            "battle-unit-shadow/g000uu0002/idle/attacker",
            "battle-unit-shadow/g000uu0003/idle/attacker",
            "battle-unit-shadow/g000uu0004/idle/attacker",
            "battle-unit-shadow/g000uu0005/idle/attacker",
            "battle-unit-shadow/g000uu0006/idle/attacker",
            "battle-unit-shadow/g000uu0007/idle/attacker"
        ]));

        Assert.That(
            File.ReadAllBytes(sb.GetPath("second.galleanpack")),
            Is.EqualTo(File.ReadAllBytes(sb.GetPath("first.galleanpack"))));
    }

    [Test]
    public void CategoryTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestModels.TryGetUpscaleModelDirectory();
        TestModels.TryGetRefineModelDirectory();

        string modelDirectory = TestModels.TryGetSurfaceModelDirectory();

        using AssetPack pack = AssetPack.Read(WritePack(sb));

        PackEnhancerOptions options = new(
            Categories: CategorySelector.FromPatterns(["unit-face"]),
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: sb.GetPath("out.galleanpack"),
            Sparse: false,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: sb.GetPath("cache"),
            ScratchDirectory: sb.RootPath,
            [EnhancementType.Upscale, EnhancementType.Surface]);

        PackEnhancerResult result = new PackEnhancer(pack, _ => { }).Enhance(options);

        Assert.That(result, Is.EqualTo(new PackEnhancerResult.Enhanced(
            sb.GetPath("out.galleanpack"), EntryCount: 1, CacheEntryCount: 1)));

        using AssetPack enhanced = AssetPack.Read(sb.GetPath("out.galleanpack"));

        Assert.That(FormatPack(enhanced), Is.EqualTo([
            "unit-face/g000uu0001 sprite 2x2 px 4x4 [b3d1f5a76c244e938f702a5c9de41b68@1]",
            "battle-unit/g000uu0001/idle/attacker animation []",
            "battle-unit/g000uu0001/idle/attacker#0 2x2 px 2x2",
            "battle-unit/g000uu0001/idle/attacker#1 2x2 px 2x2",
            "battle-unit-shadow/g000uu0001/idle/attacker animation []",
            "battle-unit-shadow/g000uu0001/idle/attacker#0 2x2 px 2x2",
            "battle-unit-shadow/g000uu0001/idle/attacker#1 2x2 px 2x2"
        ]));
    }

    [Test]
    public void CategoryEnhancementTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestModels.TryGetUpscaleModelDirectory();
        TestModels.TryGetRefineModelDirectory();
        TestModels.TryGetAlphaModelDirectory();

        string modelDirectory = TestModels.TryGetSurfaceModelDirectory();
        string cacheDirectory = sb.GetPath("cache");

        using AssetPack pack = AssetPack.Read(WritePack(sb));

        PackEnhancerOptions options = new(
            Categories: CategorySelector.FromPatterns(["battle-unit"]),
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: sb.GetPath("out.galleanpack"),
            Sparse: false,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: cacheDirectory,
            ScratchDirectory: sb.RootPath,
            [EnhancementType.Upscale, EnhancementType.Surface]);

        PackEnhancerResult result = new PackEnhancer(pack, _ => { }).Enhance(options);

        Assert.That(result, Is.EqualTo(new PackEnhancerResult.Enhanced(
            sb.GetPath("out.galleanpack"), EntryCount: 1, CacheEntryCount: 8)));

        using AssetPack enhanced = AssetPack.Read(sb.GetPath("out.galleanpack"));

        Assert.That(FormatPack(enhanced), Is.EqualTo([
            "unit-face/g000uu0001 sprite 2x2 px 2x2 []",
            "battle-unit/g000uu0001/idle/attacker animation [7638e1d713d74ed4abbbd514797df049@1,2ed23ad3f09c477fb3e30ce3bb53bbe6@2,9425579e408f470da0e3ba4a0375cff7@4,5d1e8a3792c44b6fa0e37c4f18b2d965@4]",
            "battle-unit/g000uu0001/idle/attacker#0 2x2 px 8x8 layers Normal 8x8",
            "battle-unit/g000uu0001/idle/attacker#1 2x2 px 8x8 layers Normal 8x8",
            "battle-unit-shadow/g000uu0001/idle/attacker animation []",
            "battle-unit-shadow/g000uu0001/idle/attacker#0 2x2 px 2x2",
            "battle-unit-shadow/g000uu0001/idle/attacker#1 2x2 px 2x2"
        ]));

        Assert.That(GetCachedLayerNames(cacheDirectory), Is.EqualTo([
            "none.upscale+refine+alpha.vsr.7173489d6275032f4dbc77e1.png",
            "none.upscale+refine+alpha.vsr.7173489d6275032f4dbc77e1.png",
            "none.upscale+refine.vsr.7173489d6275032f4dbc77e1.png",
            "none.upscale+refine.vsr.7173489d6275032f4dbc77e1.png",
            "none.upscale.vsr.none.png",
            "none.upscale.vsr.none.png",
            "normal.upscale+refine+alpha+surface.vsr.7173489d6275032f4dbc77e1.png",
            "normal.upscale+refine+alpha+surface.vsr.7173489d6275032f4dbc77e1.png"
        ]));
    }

    [Test]
    public void UnsupportedEnhancementTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestModels.TryGetUpscaleModelDirectory();

        string modelDirectory = TestModels.TryGetSurfaceModelDirectory();

        string packPath = sb.GetPath("base.galleanpack");
        AssetSpriteMetadata metadata = new(2, 2, 2, 2, 0, 0, Additive: false);
        AssetPack.Writer.EncodedImage frame = EncodePng(2, 2, MakePixels(2, 2, [10, 20, 30, 255]));

        using (AssetPack.Writer writer = AssetPack.Writer.Create(packPath, sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.0.9+1c2b3a4"))
        {
            writer.AddAnimation(
                new AssetAnimationKey.Water().CanonicalId,
                new AssetAnimationMetadata(1, 2, 2, 0, 0, TriggerFrame: null, SecondTriggerFrame: null),
                [new AssetPack.Writer.EncodedFrame(metadata, frame, [])],
                []);

            writer.Complete();
        }

        using AssetPack pack = AssetPack.Read(packPath);

        PackEnhancerOptions options = new(
            Categories: CategorySelector.FromPatterns(["water"]),
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: sb.GetPath("out.galleanpack"),
            Sparse: false,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: sb.GetPath("cache"),
            ScratchDirectory: sb.RootPath,
            [EnhancementType.Upscale, EnhancementType.Surface]);

        PackEnhancerResult result = new PackEnhancer(pack, _ => { }).Enhance(options);

        PackEnhancerResult.UnmatchedCategories unmatched =
            Assert.IsOfType<PackEnhancerResult.UnmatchedCategories>(result);

        Assert.That(unmatched.Patterns, Is.EqualTo(["water"]));
        Assert.That(File.Exists(sb.GetPath("out.galleanpack")), Is.False);
    }

    [Test]
    public void SelectionReportTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestModels.TryGetUpscaleModelDirectory();
        TestModels.TryGetRefineModelDirectory();
        TestModels.TryGetAlphaModelDirectory();

        string modelDirectory = TestModels.TryGetSurfaceModelDirectory();

        using AssetPack pack = AssetPack.Read(WritePack(sb));

        PackEnhancerOptions options = new(
            Categories: null,
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: null,
            Sparse: false,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: sb.GetPath("cache"),
            ScratchDirectory: sb.RootPath,
            [EnhancementType.Upscale, EnhancementType.Surface]);

        List<PackEnhancerProgress> reported = [];

        new PackEnhancer(pack, reported.Add).Enhance(options);

        Assert.That(reported, Is.EqualTo([
            new PackEnhancerProgress.Selected(
                EntryCount: 3, UpscaleCount: 3, RefineCount: 1, AlphaCount: 1, SurfaceCount: 1)
        ]));
    }

    [Test]
    public void UpscaleEnhancementTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = TestModels.TryGetUpscaleModelDirectory();
        string cacheDirectory = sb.GetPath("cache");

        using AssetPack pack = AssetPack.Read(WritePack(sb));

        PackEnhancerOptions options = new(
            Categories: null,
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: sb.GetPath("out.galleanpack"),
            Sparse: false,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: cacheDirectory,
            ScratchDirectory: sb.RootPath,
            [EnhancementType.Upscale]);

        PackEnhancerResult result = new PackEnhancer(pack, _ => { }).Enhance(options);

        Assert.That(result, Is.EqualTo(new PackEnhancerResult.Enhanced(
            sb.GetPath("out.galleanpack"), EntryCount: 3, CacheEntryCount: 5)));

        Assert.That(GetCachedFileNames(cacheDirectory), Is.EquivalentTo([
            "cpu.1f0bc9f8a85ef25fcb23faf8.none.upscale.vsr.none.png",
            "cpu.9689987f0bfbdbfae519cc8d.none.upscale.vsr.none.png",
            "cpu.1f0bc9f8a85ef25fcb23faf8.none.upscale.mosr.none.png",
            "cpu.0fe1b57a3b7b78b7443837d1.none.upscale.mosr.none.png",
            "cpu.6c4e3e5ac18bfb9119de89cb.none.upscale.mosr.none.png"
        ]));

        using AssetPack enhanced = AssetPack.Read(sb.GetPath("out.galleanpack"));

        Assert.That(FormatPack(enhanced), Is.EqualTo([
            "unit-face/g000uu0001 sprite 2x2 px 4x4 [b3d1f5a76c244e938f702a5c9de41b68@1]",
            "battle-unit/g000uu0001/idle/attacker animation [7638e1d713d74ed4abbbd514797df049@1,5d1e8a3792c44b6fa0e37c4f18b2d965@2]",
            "battle-unit/g000uu0001/idle/attacker#0 2x2 px 4x4",
            "battle-unit/g000uu0001/idle/attacker#1 2x2 px 4x4",
            "battle-unit-shadow/g000uu0001/idle/attacker animation [b3d1f5a76c244e938f702a5c9de41b68@1,b7c9f1a43d524e189f0c2a6b5d8e4713@2]",
            "battle-unit-shadow/g000uu0001/idle/attacker#0 8x8 px 16x16",
            "battle-unit-shadow/g000uu0001/idle/attacker#1 8x8 px 16x16"
        ]));
    }

    [Test]
    [Explicit]
    public void MosrUpscaleEnhancementTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = TestModels.TryGetMosrUpscaleModelDirectory();
        string cacheDirectory = sb.GetPath("cache");
        string packPath = sb.GetPath("world.galleanpack");
        AssetSpriteMetadata metadata = new(6, 6, 6, 6, 0, 0, Additive: false);

        using (AssetPack.Writer writer = AssetPack.Writer.Create(packPath, sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.0.9+1c2b3a4"))
        {
            writer.AddSprite(
                new AssetSpriteKey.Tree(WorldMapTerrain.Human, Variant: 0).CanonicalId,
                new AssetPack.Writer.EncodedFrame(
                    metadata, EncodePng(6, 6, MakePixels(6, 6, [90, 120, 60, 255])), []),
                []);

            writer.Complete();
        }

        using AssetPack pack = AssetPack.Read(packPath);

        PackEnhancerOptions options = new(
            Categories: null,
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: sb.GetPath("out.galleanpack"),
            Sparse: false,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: cacheDirectory,
            ScratchDirectory: sb.RootPath,
            [EnhancementType.Upscale]);

        PackEnhancerResult result = new PackEnhancer(pack, _ => { }).Enhance(options);

        Assert.That(result, Is.EqualTo(new PackEnhancerResult.Enhanced(
            sb.GetPath("out.galleanpack"), EntryCount: 1, CacheEntryCount: 1)));

        Assert.That(GetCachedFileNames(cacheDirectory), Is.EqualTo([
            "cpu.521c81c3d67f4fee8dbb766f.none.upscale.mosr.none.png"
        ]));

        using AssetPack enhanced = AssetPack.Read(sb.GetPath("out.galleanpack"));

        Assert.That(FormatPack(enhanced), Is.EqualTo([
            "tree/human/0 sprite 6x6 px 12x12 [b3d1f5a76c244e938f702a5c9de41b68@1]"
        ]));
    }

    [Test]
    public void RefineEnhancementTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestModels.TryGetUpscaleModelDirectory();

        string modelDirectory = TestModels.TryGetRefineModelDirectory();
        string cacheDirectory = sb.GetPath("cache");

        using AssetPack pack = AssetPack.Read(WritePack(sb));

        PackEnhancerOptions options = new(
            Categories: null,
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: sb.GetPath("out.galleanpack"),
            Sparse: false,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: cacheDirectory,
            ScratchDirectory: sb.RootPath,
            [EnhancementType.Refine]);

        PackEnhancerResult result = new PackEnhancer(pack, _ => { }).Enhance(options);

        Assert.That(result, Is.EqualTo(new PackEnhancerResult.Enhanced(
            sb.GetPath("out.galleanpack"), EntryCount: 3, CacheEntryCount: 7)));

        Assert.That(GetCachedFileNames(cacheDirectory), Is.EquivalentTo([
            "cpu.1f0bc9f8a85ef25fcb23faf8.none.upscale.vsr.none.png",
            "cpu.9689987f0bfbdbfae519cc8d.none.upscale.vsr.none.png",
            "cpu.1f0bc9f8a85ef25fcb23faf8.none.upscale.mosr.none.png",
            "cpu.0fe1b57a3b7b78b7443837d1.none.upscale.mosr.none.png",
            "cpu.6c4e3e5ac18bfb9119de89cb.none.upscale.mosr.none.png",
            "cpu.1f0bc9f8a85ef25fcb23faf8.none.upscale+refine.vsr.7173489d6275032f4dbc77e1.png",
            "cpu.9689987f0bfbdbfae519cc8d.none.upscale+refine.vsr.7173489d6275032f4dbc77e1.png"
        ]));

        using AssetPack enhanced = AssetPack.Read(sb.GetPath("out.galleanpack"));

        Assert.That(FormatPack(enhanced), Is.EqualTo([
            "unit-face/g000uu0001 sprite 2x2 px 4x4 [b3d1f5a76c244e938f702a5c9de41b68@1]",
            "battle-unit/g000uu0001/idle/attacker animation [7638e1d713d74ed4abbbd514797df049@1,2ed23ad3f09c477fb3e30ce3bb53bbe6@2,5d1e8a3792c44b6fa0e37c4f18b2d965@4]",
            "battle-unit/g000uu0001/idle/attacker#0 2x2 px 8x8",
            "battle-unit/g000uu0001/idle/attacker#1 2x2 px 8x8",
            "battle-unit-shadow/g000uu0001/idle/attacker animation [b3d1f5a76c244e938f702a5c9de41b68@1,b7c9f1a43d524e189f0c2a6b5d8e4713@2]",
            "battle-unit-shadow/g000uu0001/idle/attacker#0 8x8 px 16x16",
            "battle-unit-shadow/g000uu0001/idle/attacker#1 8x8 px 16x16"
        ]));
    }

    [Test]
    public void SurfaceEnhancementTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestModels.TryGetUpscaleModelDirectory();
        TestModels.TryGetRefineModelDirectory();
        TestModels.TryGetAlphaModelDirectory();

        string modelDirectory = TestModels.TryGetSurfaceModelDirectory();
        string cacheDirectory = sb.GetPath("cache");

        using AssetPack pack = AssetPack.Read(WritePack(sb));

        PackEnhancerOptions options = new(
            Categories: null,
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: sb.GetPath("out.galleanpack"),
            Sparse: false,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: cacheDirectory,
            ScratchDirectory: sb.RootPath,
            [EnhancementType.Surface]);

        PackEnhancerResult result = new PackEnhancer(pack, _ => { }).Enhance(options);

        Assert.That(result, Is.EqualTo(new PackEnhancerResult.Enhanced(
            sb.GetPath("out.galleanpack"), EntryCount: 3, CacheEntryCount: 11)));

        Assert.That(GetCachedFileNames(cacheDirectory), Is.EquivalentTo([
            "cpu.1f0bc9f8a85ef25fcb23faf8.none.upscale.vsr.none.png",
            "cpu.9689987f0bfbdbfae519cc8d.none.upscale.vsr.none.png",
            "cpu.1f0bc9f8a85ef25fcb23faf8.none.upscale.mosr.none.png",
            "cpu.0fe1b57a3b7b78b7443837d1.none.upscale.mosr.none.png",
            "cpu.6c4e3e5ac18bfb9119de89cb.none.upscale.mosr.none.png",
            "cpu.1f0bc9f8a85ef25fcb23faf8.none.upscale+refine.vsr.7173489d6275032f4dbc77e1.png",
            "cpu.9689987f0bfbdbfae519cc8d.none.upscale+refine.vsr.7173489d6275032f4dbc77e1.png",
            "cpu.1f0bc9f8a85ef25fcb23faf8.none.upscale+refine+alpha.vsr.7173489d6275032f4dbc77e1.png",
            "cpu.9689987f0bfbdbfae519cc8d.none.upscale+refine+alpha.vsr.7173489d6275032f4dbc77e1.png",
            "cpu.1f0bc9f8a85ef25fcb23faf8.normal.upscale+refine+alpha+surface.vsr.7173489d6275032f4dbc77e1.png",
            "cpu.9689987f0bfbdbfae519cc8d.normal.upscale+refine+alpha+surface.vsr.7173489d6275032f4dbc77e1.png"
        ]));

        using AssetPack enhanced = AssetPack.Read(sb.GetPath("out.galleanpack"));

        Assert.That(FormatPack(enhanced), Is.EqualTo([
            "unit-face/g000uu0001 sprite 2x2 px 4x4 [b3d1f5a76c244e938f702a5c9de41b68@1]",
            "battle-unit/g000uu0001/idle/attacker animation [7638e1d713d74ed4abbbd514797df049@1,2ed23ad3f09c477fb3e30ce3bb53bbe6@2,9425579e408f470da0e3ba4a0375cff7@4,5d1e8a3792c44b6fa0e37c4f18b2d965@4]",
            "battle-unit/g000uu0001/idle/attacker#0 2x2 px 8x8 layers Normal 8x8",
            "battle-unit/g000uu0001/idle/attacker#1 2x2 px 8x8 layers Normal 8x8",
            "battle-unit-shadow/g000uu0001/idle/attacker animation [b3d1f5a76c244e938f702a5c9de41b68@1,b7c9f1a43d524e189f0c2a6b5d8e4713@2]",
            "battle-unit-shadow/g000uu0001/idle/attacker#0 8x8 px 16x16",
            "battle-unit-shadow/g000uu0001/idle/attacker#1 8x8 px 16x16"
        ]));
    }

    [Test]
    public void SparseTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestModels.TryGetUpscaleModelDirectory();
        TestModels.TryGetRefineModelDirectory();
        TestModels.TryGetAlphaModelDirectory();

        string modelDirectory = TestModels.TryGetSurfaceModelDirectory();

        using AssetPack pack = AssetPack.Read(WritePack(sb));

        PackEnhancerOptions options = new(
            Categories: null,
            ModelDirectory: modelDirectory,
            SurfaceDenoisingSteps: 2,
            Device: UmbraDevice.Cpu,
            OutputPath: sb.GetPath("out.galleanpack"),
            Sparse: true,
            CreatedAt: new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            CreatedBy: "0.1.0+2d40300",
            CacheDirectory: sb.GetPath("cache"),
            ScratchDirectory: sb.RootPath,
            [EnhancementType.Upscale, EnhancementType.Surface]);

        PackEnhancerResult result = new PackEnhancer(pack, _ => { }).Enhance(options);

        Assert.IsOfType<PackEnhancerResult.Enhanced>(result);

        using AssetPack enhanced = AssetPack.Read(sb.GetPath("out.galleanpack"));

        Assert.That(enhanced.Manifest.Sparse, Is.True);
        Assert.That(enhanced.Manifest.CreatedBy, Is.EqualTo("0.1.0+2d40300"));

        Assert.That(FormatPack(enhanced), Is.EqualTo([
            "unit-face/g000uu0001 sprite 2x2 px 4x4 [b3d1f5a76c244e938f702a5c9de41b68@1]",
            "battle-unit/g000uu0001/idle/attacker animation [7638e1d713d74ed4abbbd514797df049@1,2ed23ad3f09c477fb3e30ce3bb53bbe6@2,9425579e408f470da0e3ba4a0375cff7@4,5d1e8a3792c44b6fa0e37c4f18b2d965@4]",
            "battle-unit/g000uu0001/idle/attacker#0 2x2 px 8x8 layers Normal 8x8",
            "battle-unit/g000uu0001/idle/attacker#1 2x2 px 8x8 layers Normal 8x8",
            "battle-unit-shadow/g000uu0001/idle/attacker animation [b3d1f5a76c244e938f702a5c9de41b68@1,b7c9f1a43d524e189f0c2a6b5d8e4713@2]",
            "battle-unit-shadow/g000uu0001/idle/attacker#0 8x8 px 16x16",
            "battle-unit-shadow/g000uu0001/idle/attacker#1 8x8 px 16x16"
        ]));

        Assert.That(FormatIds(enhanced), Is.EqualTo([
            "unit-face/g000uu0001",
            "battle-unit/g000uu0001/idle/attacker",
            "battle-unit-shadow/g000uu0001/idle/attacker"
        ]));
    }
}
