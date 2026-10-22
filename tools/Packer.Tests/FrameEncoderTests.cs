using System.Buffers.Binary;
using ImageMagick;

namespace Packer.Tests;

public sealed class FrameEncoderTests
{
    private static AssetSprite MakeSprite(bool additive, byte[] pixels)
    {
        int width = pixels.Length / 4;

        return new AssetSprite(new AssetSpriteMetadata(width, 1, width, 1, 0, 0, additive), pixels);
    }

    private static AssetSprite MakeGradient(bool additive, int colors)
    {
        byte[] pixels = new byte[colors * 4];

        for (int index = 0; index < colors; index++)
        {
            pixels[index * 4] = (byte)(index % 64 * 4);
            pixels[index * 4 + 1] = (byte)(index / 64 * 4);
            pixels[index * 4 + 2] = 0;
            pixels[index * 4 + 3] = additive ? (byte)0 : (byte)255;
        }

        return MakeSprite(additive, pixels);
    }

    private static byte[] Decode(AssetPack.Writer.EncodedImage image)
    {
        using MagickImage decoded = new(image.Data.Span);
        decoded.Alpha(AlphaAction.Set);

        using IUnsafePixelCollection<byte> collection = decoded.GetPixelsUnsafe();
        byte[]? pixels = collection.ToByteArray(PixelMapping.RGBA);

        return pixels ?? [];
    }

    private static int DistinctColors(byte[] pixels)
    {
        HashSet<uint> colors = [];

        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            colors.Add(BinaryPrimitives.ReadUInt32LittleEndian(pixels.AsSpan(offset)));
        }

        return colors.Count;
    }

    private static IReadOnlyList<byte> DistinctAlphas(byte[] pixels)
    {
        HashSet<byte> alphas = [];

        for (int offset = 3; offset < pixels.Length; offset += 4)
        {
            alphas.Add(pixels[offset]);
        }

        return [.. alphas.Order()];
    }

    [Test]
    public void EncodeTest()
    {
        AssetSprite sprite = MakeSprite(additive: false,
        [
            0x28, 0x6e, 0x6c, 255,
            0x7a, 0x5a, 0x3c, 128,
            0x11, 0x22, 0x33, 0,
            0x00, 0x00, 0x00, 255
        ]);

        AssetKey key = AssetKey.Parse("unit-face/g000uu0001", null);
        FrameEncoder.QuantizerPool quantizers = new();
        FrameEncoder.EncodedEntry entry = FrameEncoder.EncodeFrames([sprite], key, [], EncodingType.None, new TestSpriteFilterSource([]), quantizers);
        AssetPack.Writer.EncodedImage image = entry.Images[0];

        byte[] expected =
        [
            0x28, 0x6e, 0x6c, 255,
            0x7a, 0x5a, 0x3c, 128,
            0x00, 0x00, 0x00, 0,
            0x00, 0x00, 0x00, 255
        ];

        Assert.That(Decode(image), Is.EqualTo(expected));
        Assert.That(image.PixelWidth, Is.EqualTo(4));
        Assert.That(image.PixelHeight, Is.EqualTo(1));
        Assert.That(entry.Transforms, Is.Empty);
    }

    [Test]
    public void AdditiveTest()
    {
        AssetSprite sprite = MakeSprite(additive: true,
        [
            0x28, 0x6e, 0x6c, 0,
            0x11, 0x22, 0x33, 0
        ]);

        AssetKey key = AssetKey.Parse("unit-face/g000uu0001", null);
        AssetTransform additive = new(new Guid("268df81e-7173-4aac-8653-a10cb1e295a5"), 1);
        FrameEncoder.QuantizerPool quantizers = new();
        FrameEncoder.EncodedEntry entry = FrameEncoder.EncodeFrames([sprite], key, [additive], EncodingType.None, new TestSpriteFilterSource([]), quantizers);

        byte[] expected =
        [
            0x28, 0x6e, 0x6c, 0,
            0x11, 0x22, 0x33, 0
        ];

        Assert.That(Decode(entry.Images[0]), Is.EqualTo(expected));
    }

    [Test]
    public void PaletteCeilingTest()
    {
        AssetSprite sprite = MakeGradient(additive: false, colors: 4096);

        AssetKey key = AssetKey.Parse("battle-corpse/large/1", null);
        FrameEncoder.QuantizerPool quantizers = new();
        FrameEncoder.EncodedEntry entry = FrameEncoder.EncodeFrames([sprite], key, [], EncodingType.Palette, new TestSpriteFilterSource([]), quantizers);
        byte[] decoded = Decode(entry.Images[0]);

        Assert.That(DistinctColors(sprite.Pixels.ToArray()), Is.EqualTo(4096));
        Assert.That(DistinctColors(decoded), Is.EqualTo(256));
        Assert.That(DistinctAlphas(decoded), Is.EqualTo(new byte[] { 255 }));
    }

    [Test]
    public void AdditivePaletteCeilingTest()
    {
        AssetSprite sprite = MakeGradient(additive: true, colors: 4096);

        AssetKey key = AssetKey.Parse("battle-corpse/large/1", null);
        AssetTransform trim = new(new Guid("73de3ef1-c6ca-4304-be09-46bbc27d5446"), 1);
        FrameEncoder.QuantizerPool quantizers = new();
        FrameEncoder.EncodedEntry entry = FrameEncoder.EncodeFrames([sprite], key, [trim], EncodingType.Palette, new TestSpriteFilterSource([]), quantizers);
        byte[] decoded = Decode(entry.Images[0]);

        Assert.That(DistinctColors(decoded), Is.EqualTo(256));
        Assert.That(DistinctAlphas(decoded), Is.EqualTo(new byte[] { 0 }));
    }

    [Test]
    public void SharedPaletteTest()
    {
        AssetSprite first = MakeGradient(additive: false, colors: 4096);
        AssetSprite second = MakeGradient(additive: false, colors: 8);

        AssetKey key = AssetKey.Parse("battle-corpse/large/1", null);
        FrameEncoder.QuantizerPool quantizers = new();
        FrameEncoder.EncodedEntry entry = FrameEncoder.EncodeFrames([first, second], key, [], EncodingType.Palette, new TestSpriteFilterSource([]), quantizers);
        byte[] decodedFirst = Decode(entry.Images[0]);
        byte[] decodedSecond = Decode(entry.Images[1]);

        byte[] expected =
        [
            0x06, 0x05, 0x00, 255,
            0x06, 0x05, 0x00, 255,
            0x06, 0x05, 0x00, 255,
            0x06, 0x05, 0x00, 255,
            0x16, 0x05, 0x00, 255,
            0x16, 0x05, 0x00, 255,
            0x16, 0x05, 0x00, 255,
            0x16, 0x05, 0x00, 255
        ];

        Assert.That(decodedSecond, Is.EqualTo(expected));
        Assert.That(decodedFirst[..32], Is.EqualTo(expected));
    }

    [Test]
    public void DeterministicTest()
    {
        AssetSprite sprite = MakeGradient(additive: false, colors: 4096);

        AssetKey key = AssetKey.Parse("battle-corpse/large/1", null);
        FrameEncoder.QuantizerPool quantizers = new();
        FrameEncoder.EncodedEntry first = FrameEncoder.EncodeFrames([sprite], key, [], EncodingType.Palette, new TestSpriteFilterSource([]), quantizers);
        FrameEncoder.EncodedEntry second = FrameEncoder.EncodeFrames([sprite], key, [], EncodingType.Palette, new TestSpriteFilterSource([]), quantizers);

        AssetPack.Writer.EncodedImage firstImage = first.Images[0];
        AssetPack.Writer.EncodedImage secondImage = second.Images[0];

        Assert.That(secondImage.Data.ToArray(), Is.EqualTo(firstImage.Data.ToArray()));
    }

    [Test]
    public void TrueColorTest()
    {
        AssetSprite sprite = MakeGradient(additive: true, colors: 4096);

        AssetKey key = AssetKey.Parse("stack-unit-aura/g000uu0023/move/0", null);
        AssetTransform trim = new(new Guid("73de3ef1-c6ca-4304-be09-46bbc27d5446"), 1);
        AssetTransform additive = new(new Guid("268df81e-7173-4aac-8653-a10cb1e295a5"), 1);
        FrameEncoder.QuantizerPool quantizers = new();
        FrameEncoder.EncodedEntry entry = FrameEncoder.EncodeFrames([sprite], key, [trim, additive], EncodingType.None, new TestSpriteFilterSource([]), quantizers);

        Assert.That(Decode(entry.Images[0]), Is.EqualTo(sprite.Pixels.ToArray()));
    }

    [Test]
    public void LossyTest()
    {
        AssetSprite sprite = MakeSprite(additive: true,
        [
            0x28, 0x6e, 0x6c, 0,
            0x11, 0x22, 0x33, 0,
            0x80, 0x40, 0x20, 0,
            0xf0, 0xe0, 0xd0, 0
        ]);

        AssetKey key = AssetKey.Parse("battle-unit-aura/g000uu0099/attack/defender", null);
        AssetTransform trim = new(new Guid("73de3ef1-c6ca-4304-be09-46bbc27d5446"), 1);
        AssetTransform additive = new(new Guid("268df81e-7173-4aac-8653-a10cb1e295a5"), 1);
        FrameEncoder.QuantizerPool quantizers = new();
        FrameEncoder.EncodedEntry entry = FrameEncoder.EncodeFrames([sprite], key, [trim, additive], EncodingType.Lossy, new TestSpriteFilterSource([]), quantizers);

        byte[] expected =
        [
            0x38, 0x5f, 0x6b, 0,
            0x13, 0x24, 0x28, 0,
            0x62, 0x4b, 0x3c, 0,
            0xff, 0xd7, 0xc0, 0
        ];

        Assert.That(Decode(entry.Images[0]), Is.EqualTo(expected));
    }

    [Test]
    public void SurfaceNormalTest()
    {
        byte[] pixels =
        [
            0x80, 0x81, 0x82, 255,
            0x7d, 0x7e, 0x7f, 128,
            0x40, 0x60, 0xc0, 0,
            0xfd, 0xfe, 0xff, 255
        ];

        AssetSpriteLayer layer = new(4, 1, pixels);
        AssetPack.Writer.EncodedImage image = FrameEncoder.EncodeSurfaceLayer(layer, AssetSpriteLayerType.Normal);

        byte[] expected =
        [
            0x7a, 0x85, 0x88, 255,
            0x75, 0x7d, 0x8f, 255,
            0x87, 0x8b, 0xb8, 255,
            0xf4, 0xf5, 0xff, 255
        ];

        Assert.That(Decode(image), Is.EqualTo(expected));
    }

    [Test]
    public void SurfaceDepthTest()
    {
        byte[] pixels =
        [
            0x80, 0x81, 0x82, 255,
            0x7d, 0x7e, 0x7f, 128
        ];

        AssetSpriteLayer layer = new(2, 1, pixels);
        AssetPack.Writer.EncodedImage image = FrameEncoder.EncodeSurfaceLayer(layer, AssetSpriteLayerType.Depth);

        Assert.That(Decode(image), Is.EqualTo(pixels));
    }

    [Test]
    public void BackgroundNormalTest()
    {
        byte[] pixels =
        [
            0x80, 0x81, 0x82, 255,
            0x7d, 0x7e, 0x7f, 255,
            0x40, 0x61, 0xc2, 255,
            0xfd, 0xfe, 0xff, 255
        ];

        AssetSpriteLayer layer = new(4, 1, pixels);
        AssetPack.Writer.EncodedImage image = FrameEncoder.EncodeBackgroundLayer(layer, AssetSpriteLayerType.Normal);

        byte[] expected =
        [
            0x80, 0x80, 0x84, 255,
            0x7c, 0x80, 0x80, 255,
            0x40, 0x60, 0xc4, 255,
            0xfc, 0xff, 0xff, 255
        ];

        Assert.That(Decode(image), Is.EqualTo(expected));
    }

    [Test]
    public void BackgroundAlbedoTest()
    {
        byte[] pixels =
        [
            0x80, 0x81, 0x82, 255,
            0x7d, 0x7e, 0x7f, 255,
            0x40, 0x61, 0xc2, 255,
            0xfd, 0xfe, 0xff, 255
        ];

        AssetSpriteLayer layer = new(4, 1, pixels);
        AssetPack.Writer.EncodedImage image = FrameEncoder.EncodeBackgroundLayer(layer, AssetSpriteLayerType.Albedo);

        byte[] expected =
        [
            0x81, 0x81, 0x81, 255,
            0x7a, 0x7f, 0x8d, 255,
            0x54, 0x5f, 0x89, 255,
            0xeb, 0xfb, 0xff, 255
        ];

        Assert.That(Decode(image), Is.EqualTo(expected));
    }
}
