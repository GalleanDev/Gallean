using TorchSharp;

namespace Umbra.Tests;

public sealed class WanDitBlockTests
{
    [Test]
    public void RopeShapeTest()
    {
        using WanRope rope = WanRope.Precompute(128, end: 32);
        using torch.Tensor frequencies = rope.Build(2, 4, 4, torch.CPU);

        Assert.That(frequencies.shape, Is.EqualTo(new long[] { 32, 1, 64 }));
    }

    [Test]
    public void RotationShapeTest()
    {
        using WanRope rope = WanRope.Precompute(128, end: 32);
        using torch.Tensor frequencies = rope.Build(2, 4, 4, torch.CPU);
        using torch.Tensor input = torch.zeros([1, 32, 256]);

        using torch.Tensor rotated = WanRotation.Apply(input, frequencies, heads: 2);

        Assert.That(rotated.shape, Is.EqualTo(new long[] { 1, 32, 256 }));
    }

    [Test]
    public void NormScaleInvariantTest()
    {
        using WanNorm norm = new(4);
        using torch.Tensor input = torch.ones([1, 1, 4]).mul(3.0);

        using torch.Tensor result = norm.call(input);

        Assert.That(result.reshape(-1).data<float>().ToArray(), Is.EqualTo([1.0f, 1.0f, 1.0f, 1.0f]).Within(1e-4f));
    }

    [Test]
    public void BlockShapeTest()
    {
        using WanDitBlock block = new(dim: 256, heads: 2, feedForwardDim: 512);
        using WanRope rope = WanRope.Precompute(128, end: 32);
        using torch.Tensor frequencies = rope.Build(2, 2, 2, torch.CPU);
        using torch.Tensor input = torch.zeros([1, 8, 256]);
        using torch.Tensor context = torch.zeros([1, 12, 256]);
        using torch.Tensor modulation = torch.zeros([1, 6, 256]);

        using torch.Tensor result = block.call(input, context, modulation, frequencies);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 8, 256 }));
    }

    [Test]
    public void BlockStateDictTest()
    {
        using WanDitBlock block = new(dim: 256, heads: 2, feedForwardDim: 512);
        Dictionary<string, torch.Tensor> state = block.state_dict();

        Assert.That(state["modulation"].shape, Is.EqualTo(new long[] { 1, 6, 256 }));
        Assert.That(state["self_attn.q.weight"].shape, Is.EqualTo(new long[] { 256, 256 }));
        Assert.That(state["self_attn.norm_q.weight"].shape, Is.EqualTo(new long[] { 256 }));
        Assert.That(state["cross_attn.k.weight"].shape, Is.EqualTo(new long[] { 256, 256 }));
        Assert.That(state["ffn.0.weight"].shape, Is.EqualTo(new long[] { 512, 256 }));
        Assert.That(state["ffn.2.weight"].shape, Is.EqualTo(new long[] { 256, 512 }));
        Assert.That(state["norm3.weight"].shape, Is.EqualTo(new long[] { 256 }));
    }
}
