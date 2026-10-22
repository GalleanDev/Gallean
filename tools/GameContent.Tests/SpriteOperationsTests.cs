using Gallean.Common;
using Gallean.Tests.Common;

namespace GameContent.Tests;

public sealed class SpriteOperationsTests
{
    private static AssetSprite MakeSprite(int width, int height, params int[] gray)
    {
        return new AssetSprite(new AssetSpriteMetadata(width, height, width, height, 0, 0, Additive: false), MakePixels(gray));
    }

    private static RawBitmap MakeBitmap(int originalWidth, int originalHeight, Rectangle bounds, params int[] gray)
    {
        return new RawBitmap
        {
            OriginalWidth = originalWidth,
            OriginalHeight = originalHeight,
            Bounds = bounds,
            Data = MakePixels(gray)
        };
    }

    private static byte[] MakePixels(int[] gray)
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

        return pixels;
    }

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

                if (pixels[offset + 3] == 0)
                {
                    cells.Add("..");
                }
                else
                {
                    cells.Add($"{pixels[offset]:D2}");
                }
            }

            rows.Add(cells.ToString());
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatTransparency(AssetSprite sprite)
    {
        List<string> rows = [];
        ReadOnlySpan<byte> pixels = sprite.Pixels.Span;

        for (int y = 0; y < sprite.Metadata.PixelHeight; y++)
        {
            List<string> cells = [];

            for (int x = 0; x < sprite.Metadata.PixelWidth; x++)
            {
                int offset = ((y * sprite.Metadata.PixelWidth) + x) * 4;

                if (pixels[offset + 3] > 0)
                {
                    cells.Add("#");
                }
                else if (pixels[offset] > 0 || pixels[offset + 1] > 0 || pixels[offset + 2] > 0)
                {
                    cells.Add("x");
                }
                else
                {
                    cells.Add(".");
                }
            }

            rows.Add(string.Concat(cells));
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatFrames(IReadOnlyList<AssetSprite> frames)
    {
        List<string> rows = [];

        foreach (AssetSprite frame in frames)
        {
            string pixels = string.Join(" | ", FormatSprite(frame));
            rows.Add($"{frame.Metadata.PixelWidth}x{frame.Metadata.PixelHeight} at {frame.Metadata.OffsetX},{frame.Metadata.OffsetY}: {pixels}");
        }

        return rows;
    }

    [Test]
    public void CompositeTest()
    {
        RawBitmap first = MakeBitmap(4, 2, new Rectangle(0, 0, 2, 1), 11, 12);
        RawBitmap second = MakeBitmap(4, 2, new Rectangle(2, 1, 2, 1), 23, 24);

        Assert.That(FormatSprite(Assert.NotNull(SpriteOperations.Composite([first, second]))), Is.EqualTo(
        [
            "11 12 .. ..",
            ".. .. 23 24"
        ]));
    }

    [Test]
    public void CompositeOutsideCanvasTest()
    {
        RawBitmap part = MakeBitmap(2, 1, new Rectangle(1, 0, 2, 1), 11, 12);

        Assert.That(FormatSprite(Assert.NotNull(SpriteOperations.Composite([part]))), Is.EqualTo([".. 11"]));
    }

    [Test]
    public void CompositeWithoutPartsTest()
    {
        Assert.That(SpriteOperations.Composite([]), Is.Null);
    }

    [Test]
    public void SourceOverAtOriginTest()
    {
        AssetSprite source = MakeSprite(4, 1, 11, -1, 13, -1);
        AssetSprite destination = MakeSprite(4, 1, 91, 92, 93, 94);

        Assert.That(FormatSprite(SpriteOperations.SourceOver(source, destination, 0, 0)), Is.EqualTo(["11 92 13 94"]));
    }

    [Test]
    public void SourceOverAtOffsetTest()
    {
        AssetSprite source = MakeSprite(2, 1, 11, 12);
        AssetSprite destination = MakeSprite(4, 1, 91, 92, 93, 94);

        Assert.That(FormatSprite(SpriteOperations.SourceOver(source, destination, 3, 0)), Is.EqualTo(["91 92 93 11"]));
    }

    [Test]
    public void SourceOverPartialAlphaTest()
    {
        byte[] halfLit = [40, 40, 40, 128];
        AssetSprite source = new(new AssetSpriteMetadata(1, 1, 1, 1, 0, 0, Additive: false), halfLit);
        AssetSprite destination = MakeSprite(1, 1, 240);

        Assert.That(FormatSprite(SpriteOperations.SourceOver(source, destination, 0, 0)), Is.EqualTo(["139"]));
    }

    [Test]
    public void SourceOverDestinationCanvasTest()
    {
        AssetSprite source = new(new AssetSpriteMetadata(2, 1, 2, 1, 5, 5, Additive: false), new byte[2 * 4]);
        AssetSprite destination = new(new AssetSpriteMetadata(2, 1, 2, 1, 30, 40, Additive: false), new byte[2 * 4]);

        Assert.That(SpriteOperations.SourceOver(source, destination, 0, 0).Metadata, Is.EqualTo(new AssetSpriteMetadata(2, 1, 2, 1, 30, 40, Additive: false)));
    }

    [Test]
    public void CropTest()
    {
        AssetSprite sprite = MakeSprite(3, 3, 11, 12, 13, 21, 22, 23, 31, 32, 33);

        Assert.That(FormatSprite(SpriteOperations.Crop(sprite, 1, 1, 2, 2)), Is.EqualTo(
        [
            "22 23",
            "32 33"
        ]));
    }

    [Test]
    public void CropOriginTest()
    {
        AssetSprite sprite = new(new AssetSpriteMetadata(2, 1, 2, 1, 60, 70, Additive: false), MakePixels([11, 12]));

        Assert.That(SpriteOperations.Crop(sprite, 0, 0, 1, 1).Metadata, Is.EqualTo(new AssetSpriteMetadata(1, 1, 1, 1, 0, 0, Additive: false)));
    }

    [Test]
    public void GetContentBoxEveryFrameTest()
    {
        AssetSprite first = MakeSprite(4, 2, -1, 12, -1, -1, -1, -1, -1, -1);
        AssetSprite second = MakeSprite(4, 2, -1, -1, -1, -1, -1, -1, 23, -1);

        Assert.That(SpriteOperations.GetContentBox([first, second]), Is.EqualTo(new SpriteOperations.ContentBox(1, 0, 2, 2)));
    }

    [Test]
    public void GetContentBoxTransparentPixelsTest()
    {
        byte[] pixels = new byte[2 * 4];
        pixels[3] = 0;
        pixels[7] = 1;
        AssetSprite sprite = new(new AssetSpriteMetadata(2, 1, 2, 1, 0, 0, Additive: false), pixels);

        Assert.That(SpriteOperations.GetContentBox([sprite]), Is.EqualTo(new SpriteOperations.ContentBox(1, 0, 1, 1)));
    }

    [Test]
    public void GetContentBoxWithoutContentTest()
    {
        AssetSprite sprite = MakeSprite(2, 1, -1, -1);

        Assert.That(SpriteOperations.GetContentBox([sprite]), Is.EqualTo(SpriteOperations.ContentBox.Empty));
    }

    [Test]
    public void ClipToCanvasTest()
    {
        RawBitmap bitmap = MakeBitmap(4, 1, new Rectangle(0, 0, 4, 1), -1, 12, 13, -1);

        AssetSprite clipped = SpriteOperations.ClipToCanvas(bitmap, 4, 1, 0, 0);

        Assert.That(FormatSprite(clipped), Is.EqualTo(["12 13"]));
        Assert.That(clipped.Metadata, Is.EqualTo(new AssetSpriteMetadata(2, 1, 2, 1, 1, 0, Additive: false)));
    }

    [Test]
    public void ClipToCanvasPartlyOutsideTest()
    {
        RawBitmap bitmap = MakeBitmap(4, 1, new Rectangle(0, 0, 3, 1), 11, 12, 13);

        AssetSprite clipped = SpriteOperations.ClipToCanvas(bitmap, 2, 1, 0, 0);

        Assert.That(FormatSprite(clipped), Is.EqualTo(["11 12"]));
        Assert.That(clipped.Metadata, Is.EqualTo(new AssetSpriteMetadata(2, 1, 2, 1, 0, 0, Additive: false)));
    }

    [Test]
    public void ClipToCanvasOffsetTest()
    {
        RawBitmap bitmap = MakeBitmap(4, 2, new Rectangle(1, 1, 1, 1), 11);

        Assert.That(SpriteOperations.ClipToCanvas(bitmap, 4, 2, 2, 0).Metadata, Is.EqualTo(new AssetSpriteMetadata(1, 1, 1, 1, 3, 1, Additive: false)));
    }

    [Test]
    public void ClipToCanvasWithoutContentTest()
    {
        RawBitmap bitmap = MakeBitmap(2, 1, new Rectangle(0, 0, 2, 1), -1, -1);

        Assert.That(SpriteOperations.ClipToCanvas(bitmap, 2, 1, 0, 0).Metadata, Is.EqualTo(new AssetSpriteMetadata(1, 1, 1, 1, 0, 0, Additive: false)));
    }

    [Test]
    public void CropFramesToContentTest()
    {
        AssetSprite first = MakeSprite(4, 2, -1, 12, -1, -1, -1, -1, -1, -1);
        AssetSprite second = MakeSprite(4, 2, -1, -1, -1, -1, -1, -1, 23, -1);

        IReadOnlyList<AssetSprite> cropped = SpriteOperations.CropFramesToContent([first, second], SpriteOperations.ContentOrigin.Canvas, margin: 0);

        Assert.That(FormatFrames(cropped), Is.EqualTo(["1x1 at 1,0: 12", "1x1 at 2,1: 23"]));
    }

    [Test]
    public void CropFramesToContentMarginTest()
    {
        AssetSprite sprite = MakeSprite(4, 2, -1, 12, -1, -1, -1, -1, -1, -1);

        IReadOnlyList<AssetSprite> cropped = SpriteOperations.CropFramesToContent([sprite], SpriteOperations.ContentOrigin.Canvas, margin: 1);

        Assert.That(FormatFrames(cropped), Is.EqualTo(["3x2 at 0,0: .. 12 .. | .. .. .."]));
    }

    [Test]
    public void CropFramesToContentEmptyFrameTest()
    {
        AssetSprite first = MakeSprite(4, 2, -1, -1, 12, -1, -1, -1, -1, -1);
        AssetSprite second = MakeSprite(4, 2, -1, -1, -1, -1, -1, -1, -1, -1);

        IReadOnlyList<AssetSprite> cropped = SpriteOperations.CropFramesToContent([first, second], SpriteOperations.ContentOrigin.Canvas, margin: 0);

        Assert.That(FormatFrames(cropped), Is.EqualTo(["1x1 at 2,0: 12", "1x1 at 2,0: .."]));
    }

    [Test]
    public void CropFramesToContentCenterOriginTest()
    {
        AssetSprite sprite = MakeSprite(4, 2, -1, 12, -1, -1, -1, -1, -1, -1);

        IReadOnlyList<AssetSprite> cropped = SpriteOperations.CropFramesToContent([sprite], SpriteOperations.ContentOrigin.Center, margin: 0);

        Assert.That(FormatFrames(cropped), Is.EqualTo(["1x1 at -1,-1: 12"]));
    }

    [Test]
    public void CropFramesToContentWithoutContentTest()
    {
        AssetSprite[] frames = [MakeSprite(4, 2, -1, -1, -1, -1, -1, -1, -1, -1)];

        IReadOnlyList<AssetSprite> cropped = SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Center, margin: 0);

        Assert.That(FormatFrames(cropped), Is.EqualTo(["1x1 at -2,-1: .."]));
    }

    [Test]
    public void ContentBoxUnionTest()
    {
        SpriteOperations.ContentBox first = new(1, 2, 2, 2);
        SpriteOperations.ContentBox second = new(4, 1, 1, 1);

        Assert.That(first.Union(second), Is.EqualTo(new SpriteOperations.ContentBox(1, 1, 4, 3)));
    }

    [Test]
    public void ContentBoxUnionWithEmptyTest()
    {
        SpriteOperations.ContentBox box = new(1, 2, 2, 2);

        Assert.That(box.Union(SpriteOperations.ContentBox.Empty), Is.EqualTo(box));
        Assert.That(SpriteOperations.ContentBox.Empty.Union(box), Is.EqualTo(box));
    }

    [Test]
    public void AdditiveTest()
    {
        AssetSprite sprite = new(
            new AssetSpriteMetadata(4, 4, 4, 4, 0, 0, Additive: true),
            MakePixels([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]));

        RawBitmap bitmap = new()
        {
            OriginalWidth = 4,
            OriginalHeight = 4,
            Bounds = new Rectangle(0, 0, 4, 4),
            Additive = true,
            Data = MakePixels([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16])
        };

        Assert.That(SpriteOperations.Crop(sprite, 1, 1, 2, 2).Metadata.Additive, Is.True);
        Assert.That(SpriteOperations.CropFramesToContent([sprite], SpriteOperations.ContentOrigin.Canvas, margin: 0)[0].Metadata.Additive, Is.True);
        Assert.That(SpriteOperations.ClipToCanvas(bitmap, 8, 8, 0, 0).Metadata.Additive, Is.True);
        Assert.That(sprite.WithOffset(3, 4).Metadata.Additive, Is.True);
    }

    [Test]
    public void AdditiveDefaultTest()
    {
        AssetSprite sprite = MakeSprite(4, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        RawBitmap bitmap = MakeBitmap(4, 4, new Rectangle(0, 0, 4, 4), 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);

        Assert.That(sprite.Metadata.Additive, Is.False);
        Assert.That(SpriteOperations.Crop(sprite, 1, 1, 2, 2).Metadata.Additive, Is.False);
        Assert.That(SpriteOperations.ClipToCanvas(bitmap, 8, 8, 0, 0).Metadata.Additive, Is.False);
    }
}
