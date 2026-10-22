namespace Umbra.Tests;

public sealed class UmbraFluidBuilderTests
{
    private static UmbraFluidMap MakeSimpleMap(int width, int height, double top, double fade)
    {
        byte[] values = new byte[width * height];

        for (int y = 0; y < height; y++)
        {
            byte value = (byte)Math.Round(Math.Clamp((y - (top * height)) / (fade * height), 0, 1) * byte.MaxValue);

            for (int x = 0; x < width; x++)
            {
                values[y * width + x] = value;
            }
        }

        return new UmbraFluidMap(width, height, values);
    }

    private static IReadOnlyList<string> FormatFrame(UmbraFluidLoop loop, int index)
    {
        const string chars = " .:-=+*#%@";
        byte[] frame = loop.Frames[index];
        List<string> rows = [];

        for (int y = 0; y < loop.Height; y++)
        {
            char[] row = new char[loop.Width];

            for (int x = 0; x < loop.Width; x++)
            {
                row[x] = chars[Math.Min(frame[y * loop.Width + x] * (chars.Length - 1) / 128, chars.Length - 1)];
            }

            rows.Add(new string(row));
        }

        return rows;
    }

    private static double Difference(byte[] first, byte[] second)
    {
        double total = 0;

        for (int index = 0; index < first.Length; index++)
        {
            total += Math.Abs(first[index] - second[index]);
        }

        return total / first.Length;
    }

    [Test]
    public void GasTest()
    {
        UmbraFluidLoop loop = UmbraFluidBuilder.Build(new UmbraFluidConfiguration(
            UmbraFluidMedium.MakeGas(),
            Seed: 1,
            SimulationScale: 1.0,
            RestoreReach: 0.625,
            SourceThreshold: 0.3,
            SourceContrast: 3.0,
            SourceFrequency: 3.2,
            WarmUpSteps: 40,
            FrameCount: 6), MakeSimpleMap(16, 8, 0.5, 0.25), 16, 8);

        Assert.That(loop.Width, Is.EqualTo(16));
        Assert.That(loop.Height, Is.EqualTo(8));
        Assert.That(loop.Frames, Has.Count.EqualTo(6));
        Assert.That(FormatFrame(loop, 3), Is.EqualTo([
            "                ",
            "   .::-:      .:",
            "   ..---.    ..-",
            "  .:==+=-.    .=",
            "   :==+*=.    .=",
            "  .-=++#+:   .:=",
            ":-=++@#*=:   :-*",
            "=#*%#*+-.......-"
        ]));
    }

    [Test]
    public void LiquidTest()
    {
        UmbraFluidLoop loop = UmbraFluidBuilder.Build(new UmbraFluidConfiguration(
            UmbraFluidMedium.MakeLiquid(),
            Seed: 1,
            SimulationScale: 1.0,
            RestoreReach: 0.625,
            SourceThreshold: 0.3,
            SourceContrast: 3.0,
            SourceFrequency: 3.2,
            WarmUpSteps: 40,
            FrameCount: 6), MakeSimpleMap(16, 8, 0.5, 0.25), 16, 8);

        Assert.That(FormatFrame(loop, 3), Is.EqualTo([
            "                ",
            "                ",
            "                ",
            "                ",
            "                ",
            "  .:=*=:.    .-=",
            " -=+%@*=:    .+%",
            "-**+#@*=:    .-*"
        ]));
    }

    [Test]
    public void SeamTest()
    {
        UmbraFluidLoop loop = UmbraFluidBuilder.Build(new UmbraFluidConfiguration(
            UmbraFluidMedium.MakeGas(),
            Seed: 1,
            SimulationScale: 1.0,
            RestoreReach: 0.625,
            SourceThreshold: 0.3,
            SourceContrast: 3.0,
            SourceFrequency: 3.2,
            WarmUpSteps: 40,
            FrameCount: 6), MakeSimpleMap(16, 8, 0.5, 0.25), 16, 8);

        Assert.That(Difference(loop.Frames[5], loop.Frames[0]), Is.EqualTo(2.5546875).Within(0.005));
        Assert.That(Difference(loop.Frames[0], loop.Frames[1]), Is.EqualTo(3.359375).Within(0.005));
    }

    [Test]
    public void StillTest()
    {
        UmbraFluidLoop loop = UmbraFluidBuilder.Build(new UmbraFluidConfiguration(
            UmbraFluidMedium.MakeGas() with
            {
                VelocityDissipation = 0.0,
                DensityDissipation = 0.0,
                DensitySpread = 0.0,
                Vorticity = 0.0,
                Viscosity = 0.0,
                GravityX = 0.0,
                GravityY = 0.0,
                DriftX = 0.0,
                DriftY = 0.0,
                Gust = 0.0,
                RestoreForce = 0.0
            },
            Seed: 1,
            SimulationScale: 1.0,
            RestoreReach: 0.625,
            SourceThreshold: 0.3,
            SourceContrast: 3.0,
            SourceFrequency: 3.2,
            WarmUpSteps: 4,
            FrameCount: 3), MakeSimpleMap(16, 8, 0.5, 0.25), 16, 8);

        Assert.That(loop.Frames[1], Is.EqualTo(loop.Frames[0]));
        Assert.That(loop.Frames[2], Is.EqualTo(loop.Frames[0]));
    }

    [Test]
    public void MapTest()
    {
        byte[] values = new byte[16 * 8];

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                values[y * 16 + x] = byte.MaxValue;
            }
        }

        UmbraFluidLoop loop = UmbraFluidBuilder.Build(new UmbraFluidConfiguration(
            UmbraFluidMedium.MakeGas(),
            Seed: 1,
            SimulationScale: 1.0,
            RestoreReach: 0.625,
            SourceThreshold: 0.3,
            SourceContrast: 3.0,
            SourceFrequency: 3.2,
            WarmUpSteps: 40,
            FrameCount: 6), new UmbraFluidMap(16, 8, values), 16, 8);

        Assert.That(FormatFrame(loop, 3), Is.EqualTo([
            "*=*@%%%##++++*-.",
            "-=*+@@#*#@##+#-.",
            "---#%@@#*=+%%*+ ",
            "--#+@@@+-+=**=- ",
            "--=+@@%+..:===- ",
            "::-*#@@= ...... ",
            ":--##%++        ",
            "=++%+++-        "
        ]));
    }
}
