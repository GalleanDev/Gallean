using Gallean.Common;

namespace Packer.Tests;

public sealed class MarginFilterTests
{
    private static IReadOnlyList<string> FormatSprite(AssetSprite sprite)
    {
        List<string> rows = [];
        ReadOnlySpan<byte> pixels = sprite.Pixels.Span;

        for (int y = 0; y < sprite.Metadata.PixelHeight; y++)
        {
            JoinStringBuilder cells = new(" ");

            for (int x = 0; x < sprite.Metadata.PixelWidth; x++)
            {
                int offset = ((y * sprite.Metadata.PixelWidth) + x) * 4;

                cells.Add(pixels[offset + 3] == 0 ? ".." : $"{pixels[offset]:D2}");
            }

            rows.Add(cells.ToString());
        }

        return rows;
    }

    [Test]
    public void ApplyTest()
    {
        byte[] pixels =
        [
            10, 10, 10, 255, 20, 20, 20, 255,
            30, 30, 30, 255, 40, 40, 40, 255
        ];

        AssetSprite sprite = new(new AssetSpriteMetadata(2, 2, 2, 2, -7, -9, Additive: false), pixels);
        AssetAnimationKey.Cursor key = new(AssetAnimationKey.CursorType.Default);
        SpriteFilterContext context = new(key, new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> expanded = new MarginFilter(margin: 1).Apply([sprite], context);

        Assert.That(expanded[0].Metadata, Is.EqualTo(new AssetSpriteMetadata(4, 4, 4, 4, -8, -10, Additive: false)));
        Assert.That(FormatSprite(expanded[0]), Is.EqualTo(
        [
            ".. .. .. ..",
            ".. 10 20 ..",
            ".. 30 40 ..",
            ".. .. .. .."
        ]));
    }

    [Test]
    public void AcceptsTest()
    {
        MarginFilter filter = new(margin: 3);

        Assert.That(filter.Accepts(new AssetSpriteMetadata(24, 54, 24, 54, 0, 0, Additive: false)), Is.True);
        Assert.That(filter.Accepts(new AssetSpriteMetadata(24, 54, 96, 216, 0, 0, Additive: false)), Is.False);
    }
}
