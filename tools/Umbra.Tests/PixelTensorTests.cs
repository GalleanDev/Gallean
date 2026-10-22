using Gallean.Common;
using TorchSharp;

namespace Umbra.Tests;

public sealed class PixelTensorTests
{
    private static List<string> FormatPixels(ReadOnlyMemory<byte> pixels, int channels)
    {
        List<string> rows = [];

        for (int offset = 0; offset < pixels.Length; offset += channels)
        {
            ReadOnlySpan<byte> pixel = pixels.Span.Slice(offset, channels);
            JoinStringBuilder parts = new(",");

            foreach (byte level in pixel)
            {
                parts.Add(level.ToString());
            }

            rows.Add(parts.ToString());
        }

        return rows;
    }

    [Test]
    public void RoundTripTest()
    {
        byte[] pixels =
        [
            10, 20, 30, 0,
            255, 0, 128, 255,
            1, 2, 3, 4,
            0, 0, 0, 0
        ];

        using torch.Tensor planes = PixelTensor.ToPlanes(pixels, 2, 2, torch.CPU);

        Assert.That(planes.shape, Is.EqualTo(new long[] { 4, 2, 2 }));
        Assert.That(FormatPixels(PixelTensor.ToPixels(planes), 4), Is.EqualTo([
            "10,20,30,0",
            "255,0,128,255",
            "1,2,3,4",
            "0,0,0,0"
        ]));
    }

    [Test]
    public void EveryLevelTest()
    {
        byte[] pixels = new byte[256 * 4];

        for (int index = 0; index < 256; index++)
        {
            pixels[index * 4] = (byte)index;
            pixels[index * 4 + 1] = (byte)(255 - index);
            pixels[index * 4 + 2] = (byte)index;
            pixels[index * 4 + 3] = (byte)index;
        }

        using torch.Tensor planes = PixelTensor.ToPlanes(pixels, 256, 1, torch.CPU);

        Assert.That(PixelTensor.ToPixels(planes), Is.EqualTo(pixels));
    }

    [Test]
    public void HalfLevelTest()
    {
        using torch.Tensor planes = torch.tensor(
            [0.5f / 255.0f, 1.5f / 255.0f, 2.5f / 255.0f, 3.5f / 255.0f],
            [4, 1, 1],
            torch.ScalarType.Float32);

        Assert.That(PixelTensor.ToPixels(planes), Is.EqualTo(new byte[] { 0, 2, 2, 4 }));
    }

    [Test]
    public void ThreeChannelTest()
    {
        byte[] pixels =
        [
            10, 20, 30,
            40, 50, 60
        ];

        using torch.Tensor planes = PixelTensor.ToPlanes(pixels, 2, 1, torch.CPU);

        Assert.That(planes.shape, Is.EqualTo(new long[] { 3, 1, 2 }));
        Assert.That(PixelTensor.ToPixels(planes), Is.EqualTo(pixels));
    }
}
