using Gallean.Common;
using TorchSharp;

namespace Umbra.Tests;

public sealed class UpscaleCanvasTests
{
    private static UmbraSprite MakeSprite(int width, int height, int offsetX, int offsetY, byte[] pixels, bool additive)
    {
        return new UmbraSprite(new UmbraSpriteMetadata(width, height, width, height, offsetX, offsetY, additive), pixels);
    }

    private static List<string> FormatCanvas(torch.Tensor canvas)
    {
        List<string> rows = [];

        for (int position = 0; position < canvas.shape[0]; position++)
        {
            byte[] pixels = PixelTensor.ToPixels(canvas.select(0, position));
            int width = (int)canvas.shape[3];

            for (int y = 0; y < canvas.shape[2]; y++)
            {
                JoinStringBuilder row = new(" ");

                for (int x = 0; x < width; x++)
                {
                    int offset = (((y * width) + x) * 4);

                    row.Add($"{pixels[offset]},{pixels[offset + 1]},{pixels[offset + 2]},{pixels[offset + 3]}");
                }

                rows.Add($"{position}: {row}");
            }
        }

        return rows;
    }

    [Test]
    public void ColorSpreadTest()
    {
        UmbraSequence sequence = new(new UmbraSequenceId("battle-unit/x/idle/attacker"), [
            new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), MakeSprite(3, 3, 0, 0, [
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 10, 20, 30, 255, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            ], additive: false))
        ]);

        using torch.Tensor canvas = UpscaleCanvas.Build(UpscaleClipBuilder.Build([sequence])[0], torch.CPU);

        Assert.That(FormatCanvas(canvas), Is.EqualTo([
            "0: 10,20,30,0 10,20,30,0 10,20,30,0",
            "0: 10,20,30,0 10,20,30,255 10,20,30,0",
            "0: 10,20,30,0 10,20,30,0 10,20,30,0"
        ]));
    }

    [Test]
    public void OppositeEdgesTest()
    {
        UmbraSequence sequence = new(new UmbraSequenceId("battle-unit/x/idle/attacker"), [
            new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), MakeSprite(5, 1, 0, 0, [
                10, 20, 30, 255,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                40, 50, 60, 128
            ], additive: false))
        ]);

        using torch.Tensor canvas = UpscaleCanvas.Build(UpscaleClipBuilder.Build([sequence])[0], torch.CPU);

        Assert.That(FormatCanvas(canvas), Is.EqualTo([
            "0: 10,20,30,255 10,20,30,0 30,40,50,0 40,50,60,0 40,50,60,128"
        ]));
    }

    [Test]
    public void OutsideFrameTest()
    {
        UmbraSequence sequence = new(new UmbraSequenceId("battle-unit/x/idle/attacker"), [
            new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), MakeSprite(2, 1, 0, 0, [
                10, 20, 30, 255,
                10, 20, 30, 255
            ], additive: false)),
            new UmbraFrame(TestHelpers.MakeSpriteHash("bbbb"), MakeSprite(1, 2, 2, 0, [
                40, 50, 60, 255,
                40, 50, 60, 255
            ], additive: false))
        ]);

        using torch.Tensor canvas = UpscaleCanvas.Build(UpscaleClipBuilder.Build([sequence])[0], torch.CPU);

        Assert.That(FormatCanvas(canvas), Is.EqualTo([
            "0: 10,20,30,255 10,20,30,255 10,20,30,0",
            "0: 10,20,30,0 10,20,30,0 10,20,30,0",
            "1: 40,50,60,0 40,50,60,0 40,50,60,255",
            "1: 40,50,60,0 40,50,60,0 40,50,60,255"
        ]));
    }

    [Test]
    public void TransparentFrameTest()
    {
        UmbraSequence sequence = new(new UmbraSequenceId("battle-effect-attack/x"), [
            new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), MakeSprite(2, 1, 0, 0, [
                10, 20, 30, 0,
                40, 50, 60, 0
            ], additive: true))
        ]);

        using torch.Tensor canvas = UpscaleCanvas.Build(UpscaleClipBuilder.Build([sequence])[0], torch.CPU);

        Assert.That(FormatCanvas(canvas), Is.EqualTo([
            "0: 10,20,30,0 40,50,60,0"
        ]));
    }
}
