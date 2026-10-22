namespace Packer.Tests;

public sealed class MainMenuBackgroundOverlayFilterTests
{
    private static AssetSprite MakeOpaqueSprite(AssetSpriteMetadata metadata)
    {
        byte[] pixels = new byte[metadata.PixelWidth * metadata.PixelHeight * 4];

        Array.Fill(pixels, byte.MaxValue);

        return new AssetSprite(metadata, pixels);
    }

    private static IReadOnlyList<int> GetRowAlpha(AssetSprite frame, int y)
    {
        ReadOnlySpan<byte> pixels = frame.Pixels.Span;
        List<int> alphas = [];

        for (int x = 0; x < frame.Metadata.PixelWidth; x++)
        {
            alphas.Add(pixels[(((y * frame.Metadata.PixelWidth) + x) * 4) + 3]);
        }

        return alphas;
    }

    private static IReadOnlyList<int> GetColumnAlpha(AssetSprite frame, int x)
    {
        ReadOnlySpan<byte> pixels = frame.Pixels.Span;
        List<int> alphas = [];

        for (int y = 0; y < frame.Metadata.PixelHeight; y++)
        {
            alphas.Add(pixels[(((y * frame.Metadata.PixelWidth) + x) * 4) + 3]);
        }

        return alphas;
    }

    [Test]
    public void InteriorEdgesTest()
    {
        AssetSprite sprite = MakeOpaqueSprite(new AssetSpriteMetadata(30, 30, 60, 60, 100, 100, Additive: false));

        SpriteFilterContext context = new(new AssetAnimationKey.MainMenuBackgroundOverlay(0), new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> frames = new MainMenuBackgroundOverlayFilter().Apply([sprite], context);

        Assert.That(GetRowAlpha(frames[0], 30), Is.EqualTo([
            0, 0, 2, 4, 7, 11, 15, 21, 27, 33,
            40, 47, 55, 63, 72, 81, 90, 99, 108, 118,
            128, 137, 147, 156, 165, 174, 183, 192, 200, 208,
            208, 200, 192, 183, 174, 165, 156, 147, 137, 128,
            118, 108, 99, 90, 81, 72, 63, 55, 47, 40,
            33, 27, 21, 15, 11, 7, 4, 2, 0, 0
        ]));
    }

    [Test]
    public void ScreenEdgesTest()
    {
        AssetSprite sprite = MakeOpaqueSprite(new AssetSpriteMetadata(30, 30, 60, 60, 770, 570, Additive: false));

        SpriteFilterContext context = new(new AssetAnimationKey.MainMenuBackgroundOverlay(1), new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> frames = new MainMenuBackgroundOverlayFilter().Apply([sprite], context);

        Assert.That(GetRowAlpha(frames[0], 59), Is.EqualTo([
            0, 0, 2, 4, 7, 11, 15, 21, 27, 33,
            40, 47, 55, 63, 72, 81, 90, 99, 108, 118,
            128, 137, 147, 156, 165, 174, 183, 192, 200, 208,
            215, 222, 228, 234, 240, 244, 248, 251, 253, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255
        ]));

        Assert.That(GetColumnAlpha(frames[0], 59), Is.EqualTo([
            0, 0, 2, 4, 7, 11, 15, 21, 27, 33,
            40, 47, 55, 63, 72, 81, 90, 99, 108, 118,
            128, 137, 147, 156, 165, 174, 183, 192, 200, 208,
            215, 222, 228, 234, 240, 244, 248, 251, 253, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255
        ]));
    }
}
