using TorchSharp;

namespace Umbra.Tests;

public sealed class WanVaeBlockTests
{
    [Test]
    public void ResidualShapeTest()
    {
        using WanVaeResidualBlock block = new(8, 8);
        using torch.Tensor input = torch.zeros([1, 8, 3, 16, 16]);

        using torch.Tensor result = block.call(input);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 8, 3, 16, 16 }));
    }

    [Test]
    public void ResidualChannelsTest()
    {
        using WanVaeResidualBlock block = new(8, 16);
        using torch.Tensor input = torch.zeros([1, 8, 3, 16, 16]);

        using torch.Tensor result = block.call(input);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 16, 3, 16, 16 }));
    }

    [Test]
    public void ResidualCacheEntriesTest()
    {
        using WanVaeResidualBlock block = new(8, 8);
        using WanVaeCache cache = new(4);
        using torch.Tensor input = torch.zeros([1, 8, 3, 16, 16]);

        using torch.Tensor result = block.Forward(input, cache);

        Assert.That(cache.Position, Is.EqualTo(2));
    }

    [Test]
    public void ResidualStateDictTest()
    {
        using WanVaeResidualBlock block = new(8, 16);
        Dictionary<string, torch.Tensor> state = block.state_dict();

        Assert.That(state.Keys.Order(StringComparer.Ordinal), Is.EqualTo([
            "residual.0.gamma",
            "residual.2.bias",
            "residual.2.weight",
            "residual.3.gamma",
            "residual.6.bias",
            "residual.6.weight",
            "shortcut.bias",
            "shortcut.weight"
        ]));
    }

    [Test]
    public void AttentionShapeTest()
    {
        using WanVaeAttentionBlock block = new(8);
        using torch.Tensor input = torch.zeros([1, 8, 3, 8, 8]);

        using torch.Tensor result = block.call(input);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 8, 3, 8, 8 }));
    }
}
