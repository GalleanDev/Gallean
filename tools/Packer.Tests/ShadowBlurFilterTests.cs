namespace Packer.Tests;

public sealed class ShadowBlurFilterTests
{
    private static AssetSprite MakeSprite(AssetSpriteMetadata metadata, params int[] gray)
    {
        byte[] pixels = new byte[gray.Length * 4];

        for (int index = 0; index < gray.Length; index++)
        {
            if (gray[index] >= 0)
            {
                pixels[index * 4] = (byte)gray[index];
                pixels[(index * 4) + 1] = (byte)gray[index];
                pixels[(index * 4) + 2] = (byte)gray[index];
                pixels[(index * 4) + 3] = byte.MaxValue;
            }
        }

        return new AssetSprite(metadata, pixels);
    }

    private static IReadOnlyList<string> FormatTransparency(IReadOnlyList<AssetSprite> frames)
    {
        List<string> rows = [];

        foreach (AssetSprite frame in frames)
        {
            ReadOnlySpan<byte> pixels = frame.Pixels.Span;

            for (int y = 0; y < frame.Metadata.PixelHeight; y++)
            {
                List<string> cells = [];

                for (int x = 0; x < frame.Metadata.PixelWidth; x++)
                {
                    int offset = ((y * frame.Metadata.PixelWidth) + x) * 4;

                    if (pixels[offset + 3] > 0)
                    {
                        cells.Add("#");
                    }
                    else
                    {
                        cells.Add(".");
                    }
                }

                rows.Add(string.Concat(cells));
            }
        }

        return rows;
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
    public void TransparencyTest()
    {
        AssetSprite sprite = MakeSprite(
            new AssetSpriteMetadata(3, 3, 3, 3, 0, 0, Additive: false),
            -1, -1, -1,
            -1, 255, -1,
            -1, -1, -1);

        AssetAnimationKey.CityShadow key = new(2);
        SpriteFilterContext context = new(key, new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> blurred = new ShadowBlurFilter(1.0).Apply([sprite], context);

        Assert.That(FormatTransparency(blurred), Is.EqualTo([
            "#####",
            "#####",
            "#####",
            "#####",
            "#####"
        ]));
    }

    [Test]
    public void MetadataTest()
    {
        AssetSprite sprite = MakeSprite(
            new AssetSpriteMetadata(3, 3, 3, 3, 10, 20, Additive: false),
            -1, -1, -1,
            -1, 255, -1,
            -1, -1, -1);

        AssetAnimationKey.CityShadow key = new(2);
        SpriteFilterContext context = new(key, new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> blurred = new ShadowBlurFilter(0.7).Apply([sprite], context);

        Assert.That(GetMetadata(blurred), Is.EqualTo([new AssetSpriteMetadata(5, 5, 5, 5, 9, 19, Additive: false)]));
    }

    [Test]
    public void AdditiveTest()
    {
        AssetSprite sprite = MakeSprite(
            new AssetSpriteMetadata(3, 3, 3, 3, 0, 0, Additive: true),
            -1, -1, -1,
            -1, 255, -1,
            -1, -1, -1);

        AssetAnimationKey.CityShadow key = new(2);
        SpriteFilterContext context = new(key, new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> blurred = new ShadowBlurFilter(0.7).Apply([sprite], context);

        Assert.That(GetMetadata(blurred), Is.EqualTo([new AssetSpriteMetadata(5, 5, 5, 5, -1, -1, Additive: true)]));
    }

    [Test]
    public void EmptyFrameTest()
    {
        AssetSprite sprite = MakeSprite(
            new AssetSpriteMetadata(2, 2, 2, 2, 10, 20, Additive: false),
            -1, -1,
            -1, -1);

        AssetAnimationKey.CityShadow key = new(2);
        SpriteFilterContext context = new(key, new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> blurred = new ShadowBlurFilter(0.7).Apply([sprite], context);

        Assert.That(GetMetadata(blurred), Is.EqualTo([new AssetSpriteMetadata(1, 1, 1, 1, 7, 17, Additive: false)]));
    }

    [Test]
    public void UpscaledFrameTest()
    {
        AssetSprite sprite = MakeSprite(
            new AssetSpriteMetadata(2, 2, 4, 4, 10, 20, Additive: false),
            -1, -1, -1, -1,
            -1, 255, 255, -1,
            -1, 255, 255, -1,
            -1, -1, -1, -1);

        AssetAnimationKey.CityShadow key = new(2);
        SpriteFilterContext context = new(key, new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> blurred = new ShadowBlurFilter(0.7).Apply([sprite], context);

        Assert.That(GetMetadata(blurred), Is.EqualTo([new AssetSpriteMetadata(6, 6, 12, 12, 8, 18, Additive: false)]));
    }
}
