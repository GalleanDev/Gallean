using TorchSharp;

namespace Umbra.Tests;

public sealed class WanVaeTests
{
    private static WanVae LoadParityVae()
    {
        WanVae vae = MetaDevice.Construct(() => new WanVae(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 1));

        ModuleParameters.Load(vae, TestParity.LoadWeights("wan-vae", "vae."));
        vae.eval();

        return vae;
    }

    [Test]
    public void EncodeTest()
    {
        using WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 2);
        using torch.Tensor images = torch.zeros([1, 3, 9, 32, 32]);

        using torch.Tensor latents = vae.Encode(images);

        Assert.That(latents.shape, Is.EqualTo(new long[] { 1, 16, 3, 4, 4 }));
    }

    [Test]
    public void DecodeTest()
    {
        using WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 2);
        using torch.Tensor latents = torch.zeros([1, 16, 3, 4, 4]);

        using torch.Tensor images = vae.Decode(latents);

        Assert.That(images.shape, Is.EqualTo(new long[] { 1, 3, 9, 32, 32 }));
    }

    [Test]
    public void RoundTripTest()
    {
        using WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 2);
        using torch.Tensor images = torch.zeros([1, 3, 13, 32, 32]);

        using torch.Tensor latents = vae.Encode(images);
        using torch.Tensor restored = vae.Decode(latents);

        Assert.That(latents.shape, Is.EqualTo(new long[] { 1, 16, 4, 4, 4 }));
        Assert.That(restored.shape, Is.EqualTo(images.shape));
    }

    [Test]
    public void PrecisionTest()
    {
        using WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 2);
        using torch.Tensor images = torch.zeros([1, 3, 5, 32, 32]);

        vae.to(torch.float64);

        using torch.Tensor latents = vae.Encode(images);
        using torch.Tensor restored = vae.Decode(latents);

        Assert.That(latents.dtype, Is.EqualTo(torch.ScalarType.Float64));
        Assert.That(restored.dtype, Is.EqualTo(torch.ScalarType.Float64));
    }

    [Test]
    public void ConvolutionCountTest()
    {
        using WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 2);

        Assert.That(vae.ConvolutionCount(forEncoder: true), Is.EqualTo(26));
        Assert.That(vae.ConvolutionCount(forEncoder: false), Is.EqualTo(33));
    }

    [Test]
    public void EncodeParityTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("wan-vae");

        using WanVae vae = LoadParityVae();
        using IDisposable scope = torch.no_grad();
        using torch.Tensor encoded = vae.Encode(parity["images"]);

        Assert.That(encoded.shape, Is.EqualTo(new long[] { 1, 16, 2, 4, 4 }));
        Assert.That(TestHelpers.MaxDifference(encoded, parity["encoded"]), Is.LessThan(1e-4f));
    }

    [Test]
    public void DecodeParityTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("wan-vae");

        using WanVae vae = LoadParityVae();
        using IDisposable scope = torch.no_grad();
        using torch.Tensor decoded = vae.Decode(parity["latents"]);

        Assert.That(decoded.shape, Is.EqualTo(new long[] { 1, 3, 5, 32, 32 }));
        Assert.That(TestHelpers.MaxDifference(decoded, parity["decoded"]), Is.LessThan(1e-4f));
    }
}
