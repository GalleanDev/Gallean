using TorchSharp;

namespace Umbra.Tests;

public sealed class WanVaeEncoderTests
{
    [Test]
    public void FirstChunkTest()
    {
        using WanVaeEncoder encoder = new(dim: 8, latentChannels: 32, [1, 2, 4, 4], residualBlocks: 2, [false, true, true]);
        using WanVaeCache cache = new(64);
        using torch.Tensor input = torch.zeros([1, 3, 1, 32, 32]);

        using torch.Tensor result = encoder.Forward(input, cache);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 32, 1, 4, 4 }));
    }

    [Test]
    public void LaterChunkTest()
    {
        using WanVaeEncoder encoder = new(dim: 8, latentChannels: 32, [1, 2, 4, 4], residualBlocks: 2, [false, true, true]);
        using WanVaeCache cache = new(64);
        using torch.Tensor first = torch.zeros([1, 3, 1, 32, 32]);
        using torch.Tensor second = torch.zeros([1, 3, 4, 32, 32]);

        using torch.Tensor warmed = encoder.Forward(first, cache);

        cache.Rewind();

        using torch.Tensor result = encoder.Forward(second, cache);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 32, 1, 4, 4 }));
    }

    [Test]
    public void StateDictShapeTest()
    {
        using WanVaeEncoder encoder = new(dim: 8, latentChannels: 32, [1, 2, 4, 4], residualBlocks: 2, [false, true, true]);
        Dictionary<string, torch.Tensor> state = encoder.state_dict();

        Assert.That(state["conv1.weight"].shape, Is.EqualTo(new long[] { 8, 3, 3, 3, 3 }));
        Assert.That(state["downsamples.0.residual.2.weight"].shape, Is.EqualTo(new long[] { 8, 8, 3, 3, 3 }));
        Assert.That(state["middle.1.to_qkv.weight"].shape, Is.EqualTo(new long[] { 96, 32, 1, 1 }));
        Assert.That(state["head.2.weight"].shape, Is.EqualTo(new long[] { 32, 32, 3, 3, 3 }));
    }
}
