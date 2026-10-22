using Gallean.Common;

namespace Packer.Tests;

public sealed class CursorFilterTests
{
    private static AssetSprite MakeSprite(int offsetX, int offsetY, params string[] rows)
    {
        int width = rows[0].Length;
        byte[] pixels = new byte[width * rows.Length * 4];

        for (int y = 0; y < rows.Length; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte[] color = rows[y][x] switch
                {
                    's' => [100, 100, 100, 255],
                    'c' => [10, 90, 170, 255],
                    _ => [0, 0, 0, 0]
                };

                color.CopyTo(pixels, ((y * width) + x) * 4);
            }
        }

        return new AssetSprite(new AssetSpriteMetadata(width, rows.Length, width, rows.Length, offsetX, offsetY, Additive: false), pixels);
    }

    private static IReadOnlyList<string> FormatFrames(IReadOnlyList<AssetSprite> frames)
    {
        List<string> rows = [];

        foreach (AssetSprite frame in frames)
        {
            ReadOnlySpan<byte> pixels = frame.Pixels.Span;

            rows.Add(frame.Metadata.ToString());

            for (int y = 0; y < frame.Metadata.PixelHeight; y++)
            {
                JoinStringBuilder cells = new(" ");

                for (int x = 0; x < frame.Metadata.PixelWidth; x++)
                {
                    int offset = ((y * frame.Metadata.PixelWidth) + x) * 4;

                    cells.Add($"{pixels[offset]:D3}/{pixels[offset + 3]:D3}");
                }

                rows.Add(cells.ToString());
            }
        }

        return rows;
    }

    [Test]
    public void EraseTest()
    {
        AssetAnimationKey.Cursor key = new(AssetAnimationKey.CursorType.EnterCity);
        AssetAnimationKey.Cursor arrowKey = new(AssetAnimationKey.CursorType.Default);

        TestSpriteFilterSource source = new(new Dictionary<AssetKey, IReadOnlyList<AssetSprite>>
        {
            [arrowKey] = [MakeSprite(0, 0, "s.", "ss")]
        });

        IReadOnlyList<AssetSprite> frames =
        [
            MakeSprite(-1, -1, "....", "cs..", "css.", "...c"),
            MakeSprite(-1, -1, "....", ".s..", "cssc", "....")
        ];

        IReadOnlyList<AssetSprite> filtered = new CursorFilter().Apply(frames, new SpriteFilterContext(key, source));

        Assert.That(FormatFrames(filtered), Is.EqualTo(
        [
            "AssetSpriteMetadata { Width = 4, Height = 4, PixelWidth = 4, PixelHeight = 4, OffsetX = -1, OffsetY = -1, Additive = False, PixelScale = 1 }",
            "000/000 000/000 000/000 000/000",
            "010/255 000/000 000/000 000/000",
            "010/255 000/000 000/000 000/000",
            "000/000 000/000 000/000 010/255",
            "AssetSpriteMetadata { Width = 4, Height = 4, PixelWidth = 4, PixelHeight = 4, OffsetX = -1, OffsetY = -1, Additive = False, PixelScale = 1 }",
            "000/000 000/000 000/000 000/000",
            "000/000 000/000 000/000 000/000",
            "010/255 000/000 000/000 010/255",
            "000/000 000/000 000/000 000/000"
        ]));
    }

    [Test]
    public void OwnArtTest()
    {
        AssetAnimationKey.Cursor key = new(AssetAnimationKey.CursorType.BattleAttack);
        TestSpriteFilterSource source = new([]);
        AssetSprite frame = MakeSprite(0, 0, "ss", "ss");

        IReadOnlyList<AssetSprite> filtered = new CursorFilter().Apply([frame], new SpriteFilterContext(key, source));

        Assert.That(filtered, Is.EqualTo([frame]));
    }

    [Test]
    public void MissingArrowTest()
    {
        AssetAnimationKey.Cursor key = new(AssetAnimationKey.CursorType.EnterCity);
        TestSpriteFilterSource source = new([]);
        AssetSprite frame = MakeSprite(0, 0, "ss", "ss");
        CursorFilter filter = new();

        Assert.That(() => filter.Apply([frame], new SpriteFilterContext(key, source)), Throws.InvalidOperationException);
    }

    [Test]
    public void AcceptsTest()
    {
        CursorFilter filter = new();

        Assert.That(filter.Accepts(new AssetSpriteMetadata(30, 60, 30, 60, 0, 0, Additive: false)), Is.True);
        Assert.That(filter.Accepts(new AssetSpriteMetadata(30, 60, 120, 240, 0, 0, Additive: false)), Is.False);
    }
}
