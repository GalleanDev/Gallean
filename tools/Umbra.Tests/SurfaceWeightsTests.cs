namespace Umbra.Tests;

public sealed class SurfaceWeightsTests
{
    private static SurfaceWeights Build(WanDit dit, WanVae vae)
    {
        return new SurfaceWeights(dit.state_dict(), vae.state_dict());
    }

    [Test]
    public void DitConfigurationTest()
    {
        using WanDit dit = new(
            dim: 256,
            inChannels: 32,
            outChannels: 16,
            feedForwardDim: 512,
            textDim: 128,
            frequencyDim: 64,
            heads: 2,
            layers: 3,
            patch: (1, 2, 2));

        using WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 2);
        SurfaceWeights weights = Build(dit, vae);

        Assert.That(weights.Dim, Is.EqualTo(256));
        Assert.That(weights.InputChannels, Is.EqualTo(32));
        Assert.That(weights.OutputChannels, Is.EqualTo(16));
        Assert.That(weights.Patch, Is.EqualTo((1L, 2L, 2L)));
        Assert.That(weights.FeedForwardDim, Is.EqualTo(512));
        Assert.That(weights.TextDim, Is.EqualTo(128));
        Assert.That(weights.FrequencyDim, Is.EqualTo(64));
        Assert.That(weights.Layers, Is.EqualTo(3));

        Assert.That(weights.Heads, Is.EqualTo(2));
    }

    [Test]
    public void VaeConfigurationTest()
    {
        using WanDit dit = new(
            dim: 256,
            inChannels: 32,
            outChannels: 16,
            feedForwardDim: 512,
            textDim: 128,
            frequencyDim: 64,
            heads: 2,
            layers: 1,
            patch: (1, 2, 2));

        using WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 2);
        SurfaceWeights weights = Build(dit, vae);

        Assert.That(weights.VaeDim, Is.EqualTo(8));
        Assert.That(weights.LatentChannels, Is.EqualTo(16));
        Assert.That(weights.VaeMultipliers, Is.EqualTo([1, 2, 4, 4]));
        Assert.That(weights.VaeResidualBlocks, Is.EqualTo(2));
    }
}
