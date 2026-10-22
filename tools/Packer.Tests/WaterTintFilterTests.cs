using Gallean.Engine.Contracts;

namespace Packer.Tests;

public sealed class WaterTintFilterTests
{
    private static AssetSprite MakeSprite(byte[] pixels)
    {
        int width = pixels.Length / 4;

        return new AssetSprite(new AssetSpriteMetadata(width, 1, width, 1, 0, 0, Additive: false), pixels);
    }

    private static IReadOnlyList<byte[]> GetPixels(IReadOnlyList<AssetSprite> frames)
    {
        List<byte[]> pixels = [];

        foreach (AssetSprite frame in frames)
        {
            pixels.Add(frame.Pixels.ToArray());
        }

        return pixels;
    }

    private static IReadOnlyList<AssetSpriteMetadata> GetMetadata(IReadOnlyList<AssetSprite> frames)
    {
        List<AssetSpriteMetadata> metadata = [];

        foreach (AssetSprite frame in frames)
        {
            metadata.Add(frame.Metadata);
        }

        return metadata;
    }

    [Test]
    public void ApplyTest()
    {
        byte[] pixels =
        [
            0x28, 0x6e, 0x6c, 255,
            0xf0, 0xf4, 0xf4, 255,
            0x7a, 0x5a, 0x3c, 128,
            0x11, 0x22, 0x33, 0
        ];

        AssetSprite sprite = MakeSprite(pixels);
        AssetAnimationKey.WaterLandmark key = new(new WorldLandmarkDefinitionId("g000mg0004"));
        SpriteFilterContext context = new(key, new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> tinted = new WaterTintFilter().Apply([sprite], context);

        byte[] expected =
        [
            0x2c, 0x80, 0x8b, 255,
            0xf1, 0xf6, 0xf7, 255,
            0x7a, 0x5a, 0x3c, 128,
            0x11, 0x22, 0x33, 0
        ];

        Assert.That(GetPixels(tinted), Is.EqualTo([expected]));
        Assert.That(GetMetadata(tinted), Is.EqualTo([new AssetSpriteMetadata(4, 1, 4, 1, 0, 0, Additive: false)]));
    }

    [Test]
    public void GrayTest()
    {
        byte[] pixels =
        [
            0x00, 0x00, 0x00, 255,
            0x80, 0x80, 0x80, 255,
            0xff, 0xff, 0xff, 255
        ];

        AssetAnimationKey.WaterLandmark key = new(new WorldLandmarkDefinitionId("g000mg0004"));
        SpriteFilterContext context = new(key, new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> tinted = new WaterTintFilter().Apply([MakeSprite(pixels)], context);

        byte[] expected =
        [
            0x00, 0x00, 0x00, 255,
            0x80, 0x80, 0x80, 255,
            0xff, 0xff, 0xff, 255
        ];

        Assert.That(GetPixels(tinted), Is.EqualTo([expected]));
    }

    [Test]
    public void ShoreColorTest()
    {
        AssetAnimationKey.WaterLandmark key = new(new WorldLandmarkDefinitionId("g000mg0004"));
        SpriteFilterContext context = new(key, new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> tinted = new WaterTintFilter().Apply([MakeSprite([0x1d, 0x49, 0x4d, 255])], context);

        Assert.That(GetPixels(tinted), Is.EqualTo([new byte[] { 0x24, 0x5b, 0x62, 255 }]));
    }

    [Test]
    public void WhiteTest()
    {
        AssetAnimationKey.WaterLandmark key = new(new WorldLandmarkDefinitionId("g000mg0004"));
        SpriteFilterContext context = new(key, new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> tinted = new WaterTintFilter().Apply([MakeSprite([0xff, 0xff, 0xff, 255])], context);

        Assert.That(GetPixels(tinted), Is.EqualTo([new byte[] { 0xff, 0xff, 0xff, 255 }]));
    }
}
