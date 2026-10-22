using System.Text;
using Gallean.Engine.Contracts;
using Gallean.Tests.Common;
using ImageMagick;

namespace Packer.Tests;

public sealed class PreviewBuilderTests
{
    private static byte[] MakePixels(int pixelWidth, int pixelHeight, byte[] color)
    {
        byte[] pixels = new byte[pixelWidth * pixelHeight * 4];

        for (int index = 0; index < pixelWidth * pixelHeight; index++)
        {
            color.CopyTo(pixels, index * 4);
        }

        return pixels;
    }

    private static AssetSprite MakeSprite(int offsetX, int offsetY, byte[] color, bool additive)
    {
        AssetSpriteMetadata metadata = new(2, 2, 2, 2, offsetX, offsetY, additive);

        return new AssetSprite(metadata, MakePixels(2, 2, color));
    }

    private static AssetPack.Writer.EncodedFrame EncodeFrame(AssetSprite sprite)
    {
        AssetSpriteMetadata metadata = sprite.Metadata;
        PixelReadSettings settings = new(
            (uint)metadata.PixelWidth,
            (uint)metadata.PixelHeight,
            StorageType.Char,
            PixelMapping.RGBA);

        using MagickImage image = new(sprite.Pixels.Span, settings);
        image.Format = MagickFormat.Png32;

        AssetPack.Writer.EncodedImage encoded = new(metadata.PixelWidth, metadata.PixelHeight, image.ToByteArray());

        return new AssetPack.Writer.EncodedFrame(metadata, encoded, []);
    }

    private static void WritePack(string path, AssetAnimationKey key, IReadOnlyList<AssetSprite> frames)
    {
        List<AssetPack.Writer.EncodedFrame> encoded = [];

        foreach (AssetSprite frame in frames)
        {
            encoded.Add(EncodeFrame(frame));
        }

        using AssetPack.Writer writer = AssetPack.Writer.Create(path, sparse: false, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");

        writer.AddAnimation(
            key.CanonicalId,
            new AssetAnimationMetadata(frames.Count, 3, 3, 0, 0, TriggerFrame: null, SecondTriggerFrame: null),
            encoded,
            []);

        writer.Complete();
    }

    private static char FormatColor(byte red, byte green, byte blue, byte alpha)
    {
        if (alpha != 255)
        {
            return '!';
        }
        else if (red == 140 && green == 140 && blue == 140)
        {
            return '.';
        }
        else if (red == 38 && green == 38 && blue == 38)
        {
            return ',';
        }
        else if (red == 0 && green == 0 && blue == 0)
        {
            return 'k';
        }
        else if (red == 255 && green == 0 && blue == 0)
        {
            return 'r';
        }
        else if (red == 0 && green == 0 && blue == 255)
        {
            return 'b';
        }
        else
        {
            return '?';
        }
    }

    private static List<string> FormatPreview(string path)
    {
        using MagickImageCollection collection = new(path);
        collection.Coalesce();

        List<string> rows = [];

        for (int index = 0; index < collection.Count; index++)
        {
            IMagickImage<byte> frame = collection[index];
            int pixelWidth = (int)frame.Width;
            int pixelHeight = (int)frame.Height;

            rows.Add($"{index} {pixelWidth}x{pixelHeight}");

            using IUnsafePixelCollection<byte> pixels = frame.GetPixelsUnsafe();
            byte[] decoded = pixels.ToByteArray(PixelMapping.RGBA) ?? [];

            for (int y = 0; y < pixelHeight; y++)
            {
                StringBuilder row = new();
                row.Append(index).Append(' ');

                for (int x = 0; x < pixelWidth; x++)
                {
                    int offset = ((y * pixelWidth) + x) * 4;

                    row.Append(FormatColor(decoded[offset], decoded[offset + 1], decoded[offset + 2], decoded[offset + 3]));
                }

                rows.Add(row.ToString());
            }
        }

        return rows;
    }

    [Test]
    public void CheckerboardTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSprite sprite = MakeSprite(0, 0, [10, 20, 30, 255], additive: false);

        AssetAnimationKey.BattleUnit key = new(
            new UnitDefinitionId("G000UU0001"),
            AssetAnimationKey.BattleUnitPose.Idle,
            BattleSide.Attacker);

        WritePack(sb.GetPath("base.galleanpack"), key, [sprite]);

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cache = new(cacheDirectory, UmbraDevice.Cpu);
        UmbraSpriteHash spriteHash = sprite.ToHash();
        byte[] pixels = MakePixels(34, 4, [255, 0, 0, 0]);

        for (int index = 0; index < 4; index++)
        {
            pixels[(index * 4) + 3] = 255;
        }

        cache.Write(new EnhancementCache.Key([EnhancementType.Upscale], UpscalerType.Vsr, spriteHash, null, null), 34, 4, pixels);

        using AssetPack pack = AssetPack.Read(sb.GetPath("base.galleanpack"));

        PreviewBuilderOptions options = new(
            CategorySelector.FromPatterns(["battle-unit/g000uu0001/idle/attacker"]),
            cacheDirectory,
            UmbraDevice.Cpu,
            Transform: null,
            sb.GetPath("previews"),
            TimeSpan.FromMilliseconds(100));

        PreviewBuilderResult result = new PreviewBuilder(pack).Build(options);
        PreviewBuilderResult.Written written = Assert.IsOfType<PreviewBuilderResult.Written>(result);

        Assert.That(FormatPreview(written.OutputPaths[0]), Is.EqualTo([
            "0 34x4",
            "0 rrrr............,,,,,,,,,,,,,,,,..",
            "0 ................,,,,,,,,,,,,,,,,..",
            "0 ................,,,,,,,,,,,,,,,,..",
            "0 ................,,,,,,,,,,,,,,,,.."
        ]));
    }

    [Test]
    public void SimpleTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSprite first = MakeSprite(0, 0, [10, 20, 30, 255], additive: false);
        AssetSprite second = MakeSprite(1, 1, [40, 50, 60, 255], additive: false);

        AssetAnimationKey.BattleUnit key = new(
            new UnitDefinitionId("G000UU0001"),
            AssetAnimationKey.BattleUnitPose.Idle,
            BattleSide.Attacker);

        WritePack(sb.GetPath("base.galleanpack"), key, [first, second]);

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cache = new(cacheDirectory, UmbraDevice.Cpu);
        UmbraSpriteHash firstHash = first.ToHash();
        UmbraSpriteHash secondHash = second.ToHash();

        cache.Write(
            new EnhancementCache.Key([EnhancementType.Upscale], UpscalerType.Vsr, firstHash, null, null),
            4,
            4,
            MakePixels(4, 4, [255, 0, 0, 255]));

        cache.Write(
            new EnhancementCache.Key([EnhancementType.Upscale], UpscalerType.Vsr, secondHash, null, null),
            4,
            4,
            MakePixels(4, 4, [0, 0, 255, 255]));

        using AssetPack pack = AssetPack.Read(sb.GetPath("base.galleanpack"));

        PreviewBuilderOptions options = new(
            CategorySelector.FromPatterns(["battle-unit/g000uu0001/idle/attacker"]),
            cacheDirectory,
            UmbraDevice.Cpu,
            Transform: null,
            sb.GetPath("previews"),
            TimeSpan.FromMilliseconds(100));

        PreviewBuilderResult result = new PreviewBuilder(pack).Build(options);
        PreviewBuilderResult.Written written = Assert.IsOfType<PreviewBuilderResult.Written>(result);

        Assert.That(written.OutputPaths, Is.EqualTo([
            sb.GetPath(Path.Combine("previews", "battle-unit-g000uu0001-idle-attacker.webp"))
        ]));

        Assert.That(FormatPreview(written.OutputPaths[0]), Is.EqualTo([
            "0 6x6",
            "0 rrrr..",
            "0 rrrr..",
            "0 rrrr..",
            "0 rrrr..",
            "0 ......",
            "0 ......",
            "1 6x6",
            "1 ......",
            "1 ......",
            "1 ..bbbb",
            "1 ..bbbb",
            "1 ..bbbb",
            "1 ..bbbb"
        ]));
    }

    [Test]
    public void AdditiveTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSprite first = MakeSprite(0, 0, [10, 20, 30, 0], additive: true);
        AssetSprite second = MakeSprite(1, 1, [40, 50, 60, 0], additive: true);

        AssetAnimationKey.BattleUnit key = new(
            new UnitDefinitionId("G000UU0001"),
            AssetAnimationKey.BattleUnitPose.Idle,
            BattleSide.Attacker);

        WritePack(sb.GetPath("base.galleanpack"), key, [first, second]);

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cache = new(cacheDirectory, UmbraDevice.Cpu);
        UmbraSpriteHash firstHash = first.ToHash();
        UmbraSpriteHash secondHash = second.ToHash();

        cache.Write(
            new EnhancementCache.Key([EnhancementType.Upscale], UpscalerType.Vsr, firstHash, null, null),
            4,
            4,
            MakePixels(4, 4, [255, 0, 0, 0]));

        cache.Write(
            new EnhancementCache.Key([EnhancementType.Upscale], UpscalerType.Vsr, secondHash, null, null),
            4,
            4,
            MakePixels(4, 4, [0, 0, 255, 0]));

        using AssetPack pack = AssetPack.Read(sb.GetPath("base.galleanpack"));

        PreviewBuilderOptions options = new(
            CategorySelector.FromPatterns(["battle-unit/g000uu0001/idle/attacker"]),
            cacheDirectory,
            UmbraDevice.Cpu,
            Transform: null,
            sb.GetPath("previews"),
            TimeSpan.FromMilliseconds(100));

        PreviewBuilderResult result = new PreviewBuilder(pack).Build(options);
        PreviewBuilderResult.Written written = Assert.IsOfType<PreviewBuilderResult.Written>(result);

        Assert.That(FormatPreview(written.OutputPaths[0]), Is.EqualTo([
            "0 6x6",
            "0 rrrrkk",
            "0 rrrrkk",
            "0 rrrrkk",
            "0 rrrrkk",
            "0 kkkkkk",
            "0 kkkkkk",
            "1 6x6",
            "1 kkkkkk",
            "1 kkkkkk",
            "1 kkbbbb",
            "1 kkbbbb",
            "1 kkbbbb",
            "1 kkbbbb"
        ]));
    }

    [Test]
    public void MissingFrameTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSprite first = MakeSprite(0, 0, [10, 20, 30, 255], additive: false);
        AssetSprite second = MakeSprite(1, 1, [40, 50, 60, 255], additive: false);

        AssetAnimationKey.BattleUnit key = new(
            new UnitDefinitionId("G000UU0001"),
            AssetAnimationKey.BattleUnitPose.Idle,
            BattleSide.Attacker);

        WritePack(sb.GetPath("base.galleanpack"), key, [first, second]);

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cache = new(cacheDirectory, UmbraDevice.Cpu);
        UmbraSpriteHash firstHash = first.ToHash();

        cache.Write(
            new EnhancementCache.Key([EnhancementType.Upscale], UpscalerType.Vsr, firstHash, null, null),
            4,
            4,
            MakePixels(4, 4, [255, 0, 0, 255]));

        using AssetPack pack = AssetPack.Read(sb.GetPath("base.galleanpack"));

        PreviewBuilderOptions options = new(
            CategorySelector.FromPatterns(["battle-unit/g000uu0001/idle/attacker"]),
            cacheDirectory,
            UmbraDevice.Cpu,
            Transform: null,
            sb.GetPath("previews"),
            TimeSpan.FromMilliseconds(100));

        PreviewBuilderResult result = new PreviewBuilder(pack).Build(options);
        PreviewBuilderResult.MissingFrames missing = Assert.IsOfType<PreviewBuilderResult.MissingFrames>(result);

        Assert.That(missing.Frames, Is.EqualTo(["battle-unit/g000uu0001/idle/attacker#1"]));
        Assert.That(Directory.Exists(sb.GetPath("previews")), Is.False);
    }

    [Test]
    public void LongestOutputTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSprite sprite = MakeSprite(0, 0, [10, 20, 30, 255], additive: false);
        AssetAnimationKey.BattleUnit key = new(
            new UnitDefinitionId("G000UU0001"),
            AssetAnimationKey.BattleUnitPose.Idle,
            BattleSide.Attacker);

        WritePack(sb.GetPath("base.galleanpack"), key, [sprite]);

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cache = new(cacheDirectory, UmbraDevice.Cpu);
        UmbraSpriteHash spriteHash = sprite.ToHash();

        cache.Write(
            new EnhancementCache.Key([EnhancementType.Upscale], UpscalerType.Vsr, spriteHash, null, null),
            4,
            4,
            MakePixels(4, 4, [255, 0, 0, 255]));

        cache.Write(
            new EnhancementCache.Key([EnhancementType.Upscale, EnhancementType.Refine], UpscalerType.Vsr, spriteHash, null, RefinePromptHash.Get(key)),
            4,
            4,
            MakePixels(4, 4, [0, 0, 255, 255]));

        using AssetPack pack = AssetPack.Read(sb.GetPath("base.galleanpack"));

        PreviewBuilderOptions options = new(
            CategorySelector.FromPatterns(["battle-unit/g000uu0001/idle/attacker"]),
            cacheDirectory,
            UmbraDevice.Cpu,
            Transform: null,
            sb.GetPath("previews"),
            TimeSpan.FromMilliseconds(100));

        PreviewBuilderResult result = new PreviewBuilder(pack).Build(options);
        PreviewBuilderResult.Written written = Assert.IsOfType<PreviewBuilderResult.Written>(result);

        Assert.That(written.OutputPaths, Is.EqualTo([
            sb.GetPath(Path.Combine("previews", "battle-unit-g000uu0001-idle-attacker.webp"))
        ]));

        Assert.That(FormatPreview(written.OutputPaths[0]), Is.EqualTo([
            "0 4x4",
            "0 bbbb",
            "0 bbbb",
            "0 bbbb",
            "0 bbbb"
        ]));
    }

    [Test]
    public void UnsupportedTransformTest()
    {
        using Sandbox sb = Sandbox.Create();

        AssetSprite sprite = MakeSprite(0, 0, [10, 20, 30, 255], additive: false);
        AssetAnimationKey.BattleUnitShadow key = new(
            new UnitDefinitionId("G000UU0001"),
            AssetAnimationKey.BattleUnitPose.Idle,
            BattleSide.Attacker);

        WritePack(sb.GetPath("base.galleanpack"), key, [sprite]);

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cache = new(cacheDirectory, UmbraDevice.Cpu);
        UmbraSpriteHash spriteHash = sprite.ToHash();

        cache.Write(
            new EnhancementCache.Key([EnhancementType.Upscale], UpscalerType.Mosr, spriteHash, null, null),
            4,
            4,
            MakePixels(4, 4, [255, 0, 0, 255]));

        using AssetPack pack = AssetPack.Read(sb.GetPath("base.galleanpack"));

        PreviewBuilderOptions options = new(
            CategorySelector.FromPatterns(["battle-unit-shadow/g000uu0001/idle/attacker"]),
            cacheDirectory,
            UmbraDevice.Cpu,
            PreviewTransformType.Refine,
            sb.GetPath("previews"),
            TimeSpan.FromMilliseconds(100));

        PreviewBuilderResult result = new PreviewBuilder(pack).Build(options);
        PreviewBuilderResult.MissingFrames missing = Assert.IsOfType<PreviewBuilderResult.MissingFrames>(result);

        Assert.That(missing.Frames, Is.EqualTo(["battle-unit-shadow/g000uu0001/idle/attacker#0"]));
    }
}
