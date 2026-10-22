using Gallean.Common;
using TorchSharp;

namespace Umbra.Tests;

public sealed class PixelOperationsTests
{
    private static torch.Tensor MakeAlpha(int width, int height, Func<int, int, double> resolve)
    {
        float[] values = new float[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                values[(y * width) + x] = (float)resolve(x, y);
            }
        }

        return torch.tensor(values).reshape(1, 1, height, width);
    }

    private static List<string> FormatAlpha(torch.Tensor alpha)
    {
        List<string> rows = [];

        for (long y = 0; y < alpha.shape[2]; y++)
        {
            JoinStringBuilder row = new(" ");

            for (long x = 0; x < alpha.shape[3]; x++)
            {
                row.Add($"{Math.Round(alpha[0, 0, y, x].ToDouble() * 255.0),3}");
            }

            rows.Add(row.ToString());
        }

        return rows;
    }

    private static torch.Tensor Gray(byte[,] levels)
    {
        int height = levels.GetLength(0);
        int width = levels.GetLength(1);
        float[] planes = new float[height * width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                planes[(y * width) + x] = levels[y, x] / 255.0f;
            }
        }

        return torch.tensor(planes, [1, 1, height, width], torch.ScalarType.Float32);
    }

    private static torch.Tensor MakeFrame(byte[,] red, byte[,] alpha)
    {
        int height = red.GetLength(0);
        int width = red.GetLength(1);
        float[] planes = new float[4 * height * width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                planes[(y * width) + x] = red[y, x] / 255.0f;
                planes[(3 * height * width) + (y * width) + x] = alpha[y, x] / 255.0f;
            }
        }

        return torch.tensor(planes, [1, 4, height, width], torch.ScalarType.Float32);
    }

    private static byte[] Levels(torch.Tensor resampled)
    {
        using torch.Tensor levels = resampled.mul(255.0).round().clamp(0.0, 255.0).to(torch.ScalarType.Byte);

        return levels.reshape(-1).data<byte>().ToArray();
    }

    [Test]
    public void AntialiasAlignedEdgeTest()
    {
        using torch.Tensor alpha = MakeAlpha(4, 2, (x, _) => x < 2 ? 1.0 : 0.0);
        using torch.Tensor resolved = PixelOperations.Antialias(alpha, 2);

        Assert.That(FormatAlpha(resolved), Is.EqualTo([
            "255 255 255 255   0   0   0   0",
            "255 255 255 255   0   0   0   0",
            "255 255 255 255   0   0   0   0",
            "255 255 255 255   0   0   0   0"
        ]));
    }

    [Test]
    public void AntialiasThinLineTest()
    {
        using torch.Tensor alpha = MakeAlpha(4, 2, (x, _) => x == 1 ? 1.0 : 0.0);
        using torch.Tensor resolved = PixelOperations.Antialias(alpha, 2);

        Assert.That(FormatAlpha(resolved), Is.EqualTo([
            "  0   0 255 255   0   0   0   0",
            "  0   0 255 255   0   0   0   0",
            "  0   0 255 255   0   0   0   0",
            "  0   0 255 255   0   0   0   0"
        ]));
    }

    [Test]
    public void AntialiasFeatherTest()
    {
        using torch.Tensor alpha = MakeAlpha(4, 2, (x, _) => x switch
        {
            0 => 1.0,
            1 => 200.0 / 255.0,
            2 => 40.0 / 255.0,
            _ => 0.0
        });
        using torch.Tensor resolved = PixelOperations.Antialias(alpha, 2);

        Assert.That(FormatAlpha(resolved), Is.EqualTo([
            "255 241 214 160  80  40   0   0",
            "255 241 214 160  80  40   0   0",
            "255 241 214 160  80  40   0   0",
            "255 241 214 160  80  40   0   0"
        ]));
    }

    [Test]
    public void AntialiasTranslucentInteriorTest()
    {
        using torch.Tensor alpha = MakeAlpha(5, 5, (x, y) =>
        {
            if (x is 0 or 4 || y is 0 or 4)
            {
                return 0.0;
            }
            else if (x == 2 && y == 2)
            {
                return 100.0 / 255.0;
            }
            else
            {
                return 1.0;
            }
        });
        using torch.Tensor resolved = PixelOperations.Antialias(alpha, 2);

        Assert.That(FormatAlpha(resolved), Is.EqualTo([
            "  0   0   0   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0   0   0",
            "  0   0  80 186 233 233 186  80   0   0",
            "  0   0 186 245 226 226 245 186   0   0",
            "  0   0 233 226 168 168 226 233   0   0",
            "  0   0 233 226 168 168 226 233   0   0",
            "  0   0 186 245 226 226 245 186   0   0",
            "  0   0  80 186 233 233 186  80   0   0",
            "  0   0   0   0   0   0   0   0   0   0",
            "  0   0   0   0   0   0   0   0   0   0"
        ]));
    }

    [Test]
    public void AntialiasDiagonalTest()
    {
        using torch.Tensor alpha = MakeAlpha(6, 6, (x, y) => x <= y ? 1.0 : 0.0);
        using torch.Tensor resolved = PixelOperations.Antialias(alpha, 2);

        Assert.That(FormatAlpha(resolved), Is.EqualTo([
            "255 254  28   0   0   0   0   0   0   0   0   0",
            "255 255 144   0   0   0   0   0   0   0   0   0",
            "255 255 255 130   0   0   0   0   0   0   0   0",
            "255 255 255 255 121   0   0   0   0   0   0   0",
            "255 255 255 255 255 130   0   0   0   0   0   0",
            "255 255 255 255 255 255 121   0   0   0   0   0",
            "255 255 255 255 255 255 255 130   0   0   0   0",
            "255 255 255 255 255 255 255 255 121   0   0   0",
            "255 255 255 255 255 255 255 255 255 130   0   0",
            "255 255 255 255 255 255 255 255 255 255 144  28",
            "255 255 255 255 255 255 255 255 255 255 255 254",
            "255 255 255 255 255 255 255 255 255 255 255 255"
        ]));
    }

    [Test]
    public void AntialiasSteppedEdgeTest()
    {
        using torch.Tensor alpha = MakeAlpha(6, 8, (x, y) => x < (y < 4 ? 2 : 4) ? 1.0 : 0.0);
        using torch.Tensor resolved = PixelOperations.Antialias(alpha, 2);

        Assert.That(FormatAlpha(resolved), Is.EqualTo([
            "255 255 255 255   0   0   0   0   0   0   0   0",
            "255 255 255 255   0   0   0   0   0   0   0   0",
            "255 255 255 255   0   0   0   0   0   0   0   0",
            "255 255 255 255   0   0   0   0   0   0   0   0",
            "255 255 255 255   0   0   0   0   0   0   0   0",
            "255 255 255 255   0   0   0   0   0   0   0   0",
            "255 255 255 255  29   0   0   0   0   0   0   0",
            "255 255 255 255 162  28   0   0   0   0   0   0",
            "255 255 255 255 255 255 189  80   0   0   0   0",
            "255 255 255 255 255 255 255 186   0   0   0   0",
            "255 255 255 255 255 255 255 236   0   0   0   0",
            "255 255 255 255 255 255 255 252   0   0   0   0",
            "255 255 255 255 255 255 255 255   0   0   0   0",
            "255 255 255 255 255 255 255 255   0   0   0   0",
            "255 255 255 255 255 255 255 255   0   0   0   0",
            "255 255 255 255 255 255 255 255   0   0   0   0"
        ]));
    }

    [Test]
    public void AntialiasClearFrameTest()
    {
        using torch.Tensor alpha = MakeAlpha(4, 2, (_, _) => 0.0);
        using torch.Tensor resolved = PixelOperations.Antialias(alpha, 2);

        Assert.That(resolved.max().ToDouble(), Is.Zero);
    }

    [Test]
    public void ResizeUpscaleTest()
    {
        using torch.Tensor source = Gray(new byte[,]
        {
            { 0, 17, 34, 51 },
            { 68, 85, 102, 119 },
            { 136, 153, 170, 187 },
            { 204, 221, 238, 255 }
        });

        using torch.Tensor resampled = PixelOperations.Resize(source, 8, 8);

        Assert.That(Levels(resampled), Is.EqualTo(new byte[]
        {
            0, 0, 5, 16, 23, 34, 43, 47,
            9, 12, 21, 32, 39, 50, 59, 63,
            44, 48, 57, 68, 75, 86, 95, 99,
            85, 89, 98, 109, 116, 127, 136, 140,
            115, 119, 128, 139, 146, 157, 166, 170,
            156, 160, 169, 180, 187, 198, 207, 211,
            192, 196, 205, 216, 223, 234, 243, 246,
            208, 212, 221, 232, 239, 250, 255, 255
        }).Within(1));
    }

    [Test]
    public void ResizeDownscaleTest()
    {
        byte[,] source = new byte[8, 8];

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                source[y, x] = (byte)(((y * 8) + x) * 4);
            }
        }

        using torch.Tensor gray = Gray(source);
        using torch.Tensor resampled = PixelOperations.Resize(gray, 4, 4);

        Assert.That(Levels(resampled), Is.EqualTo(new byte[]
        {
            18, 26, 34, 42,
            80, 88, 96, 104,
            148, 156, 164, 172,
            210, 218, 226, 234
        }).Within(1));
    }

    [Test]
    public void ResizeIdentityTest()
    {
        using torch.Tensor source = Gray(new byte[,]
        {
            { 0, 17, 34, 51 },
            { 68, 85, 102, 119 },
            { 136, 153, 170, 187 },
            { 204, 221, 238, 255 }
        });

        using torch.Tensor resampled = PixelOperations.Resize(source, 4, 4);

        Assert.That(Levels(resampled), Is.EqualTo(new byte[]
        {
            0, 17, 34, 51,
            68, 85, 102, 119,
            136, 153, 170, 187,
            204, 221, 238, 255
        }));
    }

    [Test]
    public void SpreadColorTest()
    {
        using torch.Tensor first = MakeFrame(
            new byte[,] { { 255, 0 }, { 0, 0 } },
            new byte[,] { { 255, 0 }, { 0, 0 } });

        using torch.Tensor second = MakeFrame(
            new byte[,] { { 0, 0 }, { 0, 64 } },
            new byte[,] { { 0, 0 }, { 0, 255 } });

        using torch.Tensor planes = torch.cat([first, second], dim: 0);

        using torch.Tensor spread = PixelOperations.SpreadColor(planes);

        Assert.That(Levels(spread.select(0, 0).narrow(0, 0, 1)), Is.EqualTo(new byte[] { 255, 255, 255, 255 }));
        Assert.That(Levels(spread.select(0, 1).narrow(0, 0, 1)), Is.EqualTo(new byte[] { 64, 64, 64, 64 }));
        Assert.That(Levels(spread.narrow(1, 3, 1)), Is.EqualTo(new byte[] { 255, 0, 0, 0, 0, 0, 0, 255 }));
    }
}
