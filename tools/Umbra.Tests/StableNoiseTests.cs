using TorchSharp;

namespace Umbra.Tests;

public sealed class StableNoiseTests
{
    private static float[] Format(torch.Tensor tensor, int count)
    {
        using torch.Tensor flat = tensor.reshape(-1).narrow(0, 0, count).to(torch.ScalarType.Float32);

        return flat.data<float>().ToArray();
    }

    [Test]
    public void GenerateSimpleTest()
    {
        using torch.Tensor noise = StableNoise.Generate(0, [8], torch.ScalarType.Float32, torch.CPU);

        Assert.That(Format(noise, 8), Is.EqualTo([
            1.5409961f, -0.2934289f, -2.1787894f, 0.5684313f,
            -1.0845224f, -1.3985955f, 0.4033468f, 0.8380263f
        ]).Within(1e-6));
    }

    [Test]
    public void GenerateSeedTest()
    {
        using torch.Tensor noise = StableNoise.Generate(1, [8], torch.ScalarType.Float32, torch.CPU);

        Assert.That(Format(noise, 4), Is.EqualTo([
            0.66135216f, 0.2669241f, 0.06167726f, 0.6213173f
        ]).Within(1e-6));
    }

    [Test]
    public void GenerateDtypeTest()
    {
        using torch.Tensor noise = StableNoise.Generate(0, [8], torch.ScalarType.BFloat16, torch.CPU);

        Assert.That(Format(noise, 4), Is.EqualTo([
            1.5390625f, -0.29296875f, -2.171875f, 0.5703125f
        ]).Within(1e-6));
    }

    [Test]
    public void GenerateRepeatTest()
    {
        using torch.Tensor first = StableNoise.Generate(7, [4, 9], torch.ScalarType.Float32, torch.CPU);
        using torch.Tensor second = StableNoise.Generate(7, [4, 9], torch.ScalarType.Float32, torch.CPU);

        Assert.That(Format(second, 36), Is.EqualTo(Format(first, 36)));
    }

    [Test]
    public void GenerateProductionShapeTest()
    {
        using torch.Tensor noise = StableNoise.Generate(0, [1, 16, 4, 64, 64], torch.ScalarType.Float32, torch.CPU);

        Assert.That(Format(noise, 8), Is.EqualTo([
            -1.1258398f, -1.1523602f, -0.2505786f, -0.4338788f,
            0.8487104f, 0.6920092f, -0.3160128f, -2.1152194f
        ]).Within(1e-6));
    }
}
