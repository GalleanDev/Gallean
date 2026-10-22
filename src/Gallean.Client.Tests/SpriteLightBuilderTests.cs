using System.Globalization;
using SkiaSharp;

namespace Gallean.Client.Tests;

public sealed class SpriteLightBuilderTests
{
    private static AssetSprite<SKImage> CreateSprite(int pixelWidth, int pixelHeight, IReadOnlyList<(SKRectI Area, SKColor Color)> blocks)
    {
        byte[] pixels = new byte[pixelWidth * pixelHeight * 4];

        foreach ((SKRectI area, SKColor color) in blocks)
        {
            for (int y = area.Top; y < area.Bottom; y++)
            {
                for (int x = area.Left; x < area.Right; x++)
                {
                    int offset = (y * pixelWidth + x) * 4;

                    pixels[offset] = color.Blue;
                    pixels[offset + 1] = color.Green;
                    pixels[offset + 2] = color.Red;
                }
            }
        }

        AssetSpriteMetadata metadata = new(pixelWidth / 2, pixelHeight / 2, pixelWidth, pixelHeight, 0, 0, Additive: true);

        SKImageInfo info = new(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);

        return new AssetSprite<SKImage>(metadata, SKImage.FromPixelCopy(info, pixels));
    }

    private static IReadOnlyList<string> Format(IReadOnlyList<IReadOnlyList<SpriteLight>> frames)
    {
        List<string> rows = [];

        for (int frame = 0; frame < frames.Count; frame++)
        {
            foreach (SpriteLight light in frames[frame])
            {
                rows.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"{frame}: {light.Position.X:F2},{light.Position.Y:F2} r{light.Radius:F2} {light.Radiance.X:F2} {light.Radiance.Y:F2} {light.Radiance.Z:F2} p{light.Power:F2}"));
            }
        }

        return rows;
    }

    [Test]
    public void DarkTest()
    {
        AssetSprite<SKImage> sprite = CreateSprite(40, 20, []);

        Assert.That(SpriteLightBuilder.Build([sprite]), Is.EqualTo([Array.Empty<SpriteLight>()]));
    }

    [Test]
    public void SingleGlowTest()
    {
        AssetSprite<SKImage> sprite = CreateSprite(40, 20, [(new SKRectI(4, 4, 8, 8), new SKColor(255, 128, 0))]);

        Assert.That(Format(SpriteLightBuilder.Build([sprite])), Is.EqualTo(["0: 2.83,3.00 r1.62 9.42 2.07 0.00 p3.48"]));
    }

    [Test]
    public void SeparateGlowsTest()
    {
        AssetSprite<SKImage> sprite = CreateSprite(40, 20, [(new SKRectI(4, 4, 8, 8), new SKColor(255, 0, 0)), (new SKRectI(30, 12, 34, 16), new SKColor(0, 0, 255))]);

        Assert.That(Format(SpriteLightBuilder.Build([sprite])), Is.EqualTo([
            "0: 16.00,7.00 r0.79 0.00 0.00 3.14 p0.23",
            "0: 3.00,3.00 r0.79 3.14 0.00 0.00 p0.67"
        ]));
    }

    [Test]
    public void NeighborGlowsTest()
    {
        AssetSprite<SKImage> sprite = CreateSprite(40, 20, [(new SKRectI(34, 2, 38, 6), new SKColor(225, 225, 225)), (new SKRectI(33, 7, 36, 10), new SKColor(225, 225, 225))]);

        Assert.That(Format(SpriteLightBuilder.Build([sprite])), Is.EqualTo(["0: 17.75,2.75 r2.53 7.16 7.16 7.16 p7.16"]));
    }

    [Test]
    public void AnimationTest()
    {
        AssetSprite<SKImage> apart = CreateSprite(40, 20, [(new SKRectI(4, 8, 8, 12), new SKColor(255, 255, 255)), (new SKRectI(11, 8, 15, 12), new SKColor(255, 255, 255))]);
        AssetSprite<SKImage> close = CreateSprite(40, 20, [(new SKRectI(4, 8, 8, 12), new SKColor(255, 255, 255)), (new SKRectI(8, 8, 12, 12), new SKColor(255, 255, 255))]);

        Assert.That(Format(SpriteLightBuilder.Build([apart, close])), Is.EqualTo(["0: 4.17,5.00 r3.54 9.42 9.42 9.42 p9.42", "1: 4.17,5.00 r2.38 9.42 9.42 9.42 p9.42"]));
    }
}
