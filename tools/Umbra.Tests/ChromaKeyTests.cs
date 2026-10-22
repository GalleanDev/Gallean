namespace Umbra.Tests;

public sealed class ChromaKeyTests
{
    [Test]
    public void BlueArtTest()
    {
        byte[] pixels =
        [
            0, 71, 187, 255
        ];
        UmbraSprite sprite = new(new UmbraSpriteMetadata(1, 1, 1, 1, 0, 0, false), pixels);

        Assert.That(ChromaKey.Select(sprite), Is.EqualTo(ChromaKey.Green));
    }

    [Test]
    public void GreenArtTest()
    {
        byte[] pixels =
        [
            0, 177, 64, 255
        ];
        UmbraSprite sprite = new(new UmbraSpriteMetadata(1, 1, 1, 1, 0, 0, false), pixels);

        Assert.That(ChromaKey.Select(sprite), Is.EqualTo(ChromaKey.Blue));
    }

    [Test]
    public void NeutralArtTest()
    {
        byte[] pixels =
        [
            200, 90, 60, 255
        ];
        UmbraSprite sprite = new(new UmbraSpriteMetadata(1, 1, 1, 1, 0, 0, false), pixels);

        Assert.That(ChromaKey.Select(sprite), Is.EqualTo(ChromaKey.Blue));
    }

    [Test]
    public void MarginTest()
    {
        byte[] pixels =
        [
            0, 71, 187, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255
        ];
        UmbraSprite sprite = new(new UmbraSpriteMetadata(4, 2, 4, 2, 0, 0, false), pixels);

        Assert.That(ChromaKey.Select(sprite), Is.EqualTo(ChromaKey.Green));
    }

    [Test]
    public void NarrowMarginTest()
    {
        byte[] pixels =
        [
            0, 71, 187, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255
        ];
        UmbraSprite sprite = new(new UmbraSpriteMetadata(4, 3, 4, 3, 0, 0, false), pixels);

        Assert.That(ChromaKey.Select(sprite), Is.EqualTo(ChromaKey.Blue));
    }

    [Test]
    public void TransparentArtTest()
    {
        byte[] pixels =
        [
            0, 71, 187, 0,
            200, 90, 60, 255
        ];
        UmbraSprite sprite = new(new UmbraSpriteMetadata(2, 1, 2, 1, 0, 0, false), pixels);

        Assert.That(ChromaKey.Select(sprite), Is.EqualTo(ChromaKey.Blue));
    }
}
