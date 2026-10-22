using Gallean.Common;
using Gallean.Engine.Contracts;

namespace Packer.Tests;

public sealed class AlphaContourFilterTests
{
    private static AssetSprite MakeSprite(params string[] rows)
    {
        int width = rows[0].Length;
        byte[] pixels = new byte[width * rows.Length * 4];

        for (int y = 0; y < rows.Length; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (rows[y][x] == '#')
                {
                    int offset = ((y * width) + x) * 4;

                    pixels[offset] = 200;
                    pixels[offset + 1] = 100;
                    pixels[offset + 2] = 50;
                    pixels[offset + 3] = byte.MaxValue;
                }
            }
        }

        return new AssetSprite(new AssetSpriteMetadata(width / 4, rows.Length / 4, width, rows.Length, 0, 0, Additive: false), pixels);
    }

    private static IReadOnlyList<string> FormatChannel(IReadOnlyList<AssetSprite> frames, int channel)
    {
        List<string> rows = [];

        foreach (AssetSprite frame in frames)
        {
            ReadOnlySpan<byte> pixels = frame.Pixels.Span;

            for (int y = 0; y < frame.Metadata.PixelHeight; y++)
            {
                JoinStringBuilder cells = new(" ");

                for (int x = 0; x < frame.Metadata.PixelWidth; x++)
                {
                    cells.Add($"{pixels[(((y * frame.Metadata.PixelWidth) + x) * 4) + channel],3}");
                }

                rows.Add(cells.ToString());
            }
        }

        return rows;
    }

    [Test]
    public void WavyEdgeTest()
    {
        AssetSprite sprite = MakeSprite(
            "############........",
            "############........",
            "#############.......",
            "#############.......",
            "############........",
            "############........",
            "#############.......",
            "#############.......",
            "############........",
            "############........",
            "#############.......",
            "#############.......",
            "############........",
            "############........",
            "#############.......",
            "#############.......");

        AssetAnimationKey.BattleUnit key = new(new UnitDefinitionId("g000uu0001"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker);
        SpriteFilterContext context = new(key, new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> filtered = new AlphaContourFilter().Apply([sprite], context);

        Assert.That(FormatChannel(filtered, 3), Is.EqualTo([
            "255 255 255 255 255 255 255 255 255 255 255 250  55   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255  61   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255 165   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255 170   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255  74   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255  76   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255 177   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255 177   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255  79   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255  94   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255 167   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255 169   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255 102   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255 108   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255 185   0   0   0   0   0   0   0",
            "255 255 255 255 255 255 255 255 255 255 255 255 192   4   0   0   0   0   0   0"
        ]));
        Assert.That(FormatChannel(filtered, 0), Is.EqualTo([
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0",
            "200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200 200   0   0"
        ]));
    }

    [Test]
    public void ThinLineTest()
    {
        AssetSprite sprite = MakeSprite(
            "........####........",
            "........####........",
            ".........####.......",
            ".........####.......",
            "........####........",
            "........####........",
            ".........####.......",
            ".........####.......",
            "........####........",
            "........####........",
            ".........####.......",
            ".........####.......",
            "........####........",
            "........####........",
            ".........####.......",
            ".........####.......");

        AssetAnimationKey.BattleUnit key = new(new UnitDefinitionId("g000uu0001"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker);
        SpriteFilterContext context = new(key, new TestSpriteFilterSource([]));

        IReadOnlyList<AssetSprite> filtered = new AlphaContourFilter().Apply([sprite], context);

        Assert.That(FormatChannel(filtered, 3), Is.EqualTo([
            "  0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0   0 255 255 255 255   0   0   0   0   0   0   0"
        ]));
    }
}
