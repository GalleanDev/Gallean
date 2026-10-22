using TorchSharp;

namespace Umbra.Tests;

public sealed class WanVaeDecoderTests
{
    [Test]
    public void FirstChunkTest()
    {
        using WanVaeDecoder decoder = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 2, [true, true, false]);
        using WanVaeCache cache = new(64);
        using torch.Tensor input = torch.zeros([1, 16, 1, 4, 4]);

        using torch.Tensor result = decoder.Forward(input, cache);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 3, 1, 32, 32 }));
    }

    [Test]
    public void LaterChunkTest()
    {
        using WanVaeDecoder decoder = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 2, [true, true, false]);
        using WanVaeCache cache = new(64);
        using torch.Tensor first = torch.zeros([1, 16, 1, 4, 4]);
        using torch.Tensor second = torch.zeros([1, 16, 1, 4, 4]);

        using torch.Tensor warmed = decoder.Forward(first, cache);

        cache.Rewind();

        using torch.Tensor result = decoder.Forward(second, cache);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 3, 4, 32, 32 }));
    }

    [Test]
    public void StateDictShapeTest()
    {
        using WanVaeDecoder decoder = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 2, [true, true, false]);
        Dictionary<string, torch.Tensor> state = decoder.state_dict();

        Assert.That(state["conv1.weight"].shape, Is.EqualTo(new long[] { 32, 16, 3, 3, 3 }));
        Assert.That(state["middle.1.to_qkv.weight"].shape, Is.EqualTo(new long[] { 96, 32, 1, 1 }));
        Assert.That(state["head.2.weight"].shape, Is.EqualTo(new long[] { 3, 8, 3, 3, 3 }));
    }
}
