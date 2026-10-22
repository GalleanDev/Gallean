using TorchSharp;

namespace Umbra.Tests;

public sealed class MosrNetworkTests
{
    private static MosrOptions TinyOptions()
    {
        return new MosrOptions(
            InChannels: 3,
            OutChannels: 3,
            Scale: 2,
            BlockCount: 2,
            Dimension: 8,
            KernelSize: 7,
            ExpansionRatio: 1.5,
            ConvRatio: 1.0,
            SampleGroups: 4);
    }

    [Test]
    public void StateDictTest()
    {
        using MosrNetwork network = new(MosrUpscaleWeights.Options);
        Dictionary<string, torch.Tensor> state = network.state_dict();

        Assert.That(state, Has.Count.EqualTo(212));
        Assert.That(state["gblocks.0.weight"].shape, Is.EqualTo(new long[] { 64, 3, 3, 3 }));
        Assert.That(state["gblocks.1.norm.weight"].shape, Is.EqualTo(new long[] { 64 }));
        Assert.That(state["gblocks.1.fc1.weight"].shape, Is.EqualTo(new long[] { 192, 64, 3, 3 }));
        Assert.That(state["gblocks.1.conv.weight"].shape, Is.EqualTo(new long[] { 64, 1, 7, 7 }));
        Assert.That(state["gblocks.1.fc2.weight"].shape, Is.EqualTo(new long[] { 64, 96, 3, 3 }));
        Assert.That(state["gblocks.24.fc2.weight"].shape, Is.EqualTo(new long[] { 64, 96, 3, 3 }));
        Assert.That(state["gblocks.25.weight"].shape, Is.EqualTo(new long[] { 128, 64, 3, 3 }));
        Assert.That(state["gblocks.27.weight"].shape, Is.EqualTo(new long[] { 64, 128, 3, 3 }));
        Assert.That(state["gblocks.29.weight"].shape, Is.EqualTo(new long[] { 64, 64, 1, 1 }));
        Assert.That(state["shortcut.block.0.weight"].shape, Is.EqualTo(new long[] { 64, 3, 3, 3 }));
        Assert.That(state["shortcut.block.2.weight"].shape, Is.EqualTo(new long[] { 64, 64, 3, 3 }));
        Assert.That(state["shortcut.conv11.weight"].shape, Is.EqualTo(new long[] { 64, 3, 1, 1 }));
        Assert.That(state["upsampler.offset.weight"].shape, Is.EqualTo(new long[] { 32, 64, 1, 1 }));
        Assert.That(state["upsampler.scope.weight"].shape, Is.EqualTo(new long[] { 32, 64, 1, 1 }));
        Assert.That(state["upsampler.end_conv.weight"].shape, Is.EqualTo(new long[] { 3, 64, 1, 1 }));
        Assert.That(state["upsampler.init_pos"].shape, Is.EqualTo(new long[] { 1, 32, 1, 1 }));
    }

    [Test]
    public void InitialPositionsTest()
    {
        using torch.Tensor positions = MosrDySample.BuildInitialPositions(2, 4);
        float[] values = positions.reshape(-1).data<float>().ToArray();

        Assert.That(positions.shape, Is.EqualTo(new long[] { 1, 32, 1, 1 }));
        Assert.That(values, Is.EqualTo([
            -0.25f, 0.25f, -0.25f, 0.25f, -0.25f, 0.25f, -0.25f, 0.25f,
            -0.25f, 0.25f, -0.25f, 0.25f, -0.25f, 0.25f, -0.25f, 0.25f,
            -0.25f, -0.25f, 0.25f, 0.25f, -0.25f, -0.25f, 0.25f, 0.25f,
            -0.25f, -0.25f, 0.25f, 0.25f, -0.25f, -0.25f, 0.25f, 0.25f
        ]));
    }

    [Test]
    public void TinyForwardTest()
    {
        using MosrNetwork network = new(TinyOptions());
        using torch.Tensor input = torch.rand([1, 3, 16, 24]);

        using (torch.no_grad())
        {
            using torch.Tensor output = network.call(input);

            Assert.That(output.shape, Is.EqualTo(new long[] { 1, 3, 32, 48 }));
        }
    }

    [Test]
    public void DeterministicTest()
    {
        using MosrNetwork network = new(TinyOptions());
        using torch.Tensor input = torch.rand([1, 3, 16, 16]);

        using (torch.no_grad())
        {
            using torch.Tensor first = network.call(input);
            using torch.Tensor second = network.call(input);

            Assert.That(first.allclose(second, rtol: 0, atol: 0), Is.True);
        }
    }
}
