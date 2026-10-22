using TorchSharp;

namespace Umbra.Tests;

public sealed class BasicVsrNetworkTests
{
    private static torch.Tensor Upsample(BasicVsrNetwork network, torch.Tensor clip)
    {
        long[] shape = [clip.shape[0], clip.shape[1], clip.shape[2], clip.shape[3] * 4, clip.shape[4] * 4];
        torch.Tensor output = torch.zeros(shape, clip.dtype);

        using (torch.no_grad())
        {
            network.Upsample(clip, (index, frame) =>
            {
                torch.Tensor target = output.select(1, index);

                target.copy_(frame);
            });
        }

        return output;
    }

    [Test]
    public void StateDictTest()
    {
        using BasicVsrNetwork network = new(64, 7, TestContext.CurrentContext.WorkDirectory, 128);
        Dictionary<string, torch.Tensor> state = network.state_dict();

        Assert.That(state, Has.Count.EqualTo(274));
        Assert.That(state["backbone.backward_1.main.0.weight"].shape, Is.EqualTo(new long[] { 64, 128, 3, 3 }));
        Assert.That(state["backbone.forward_1.main.0.weight"].shape, Is.EqualTo(new long[] { 64, 192, 3, 3 }));
        Assert.That(state["backbone.backward_2.main.0.weight"].shape, Is.EqualTo(new long[] { 64, 256, 3, 3 }));
        Assert.That(state["backbone.forward_2.main.0.weight"].shape, Is.EqualTo(new long[] { 64, 320, 3, 3 }));
        Assert.That(state["upconv1.weight"].shape, Is.EqualTo(new long[] { 256, 64, 3, 3 }));
        Assert.That(state["upconv2.weight"].shape, Is.EqualTo(new long[] { 256, 64, 3, 3 }));
        Assert.That(state["conv_last.weight"].shape, Is.EqualTo(new long[] { 3, 64, 3, 3 }));
    }

    [Test]
    public void TinyForwardTest()
    {
        using BasicVsrNetwork network = new(8, 1, TestContext.CurrentContext.WorkDirectory, 128);
        using torch.Tensor clip = torch.rand([1, 2, 3, 64, 64]);
        using torch.Tensor output = Upsample(network, clip);

        Assert.That(output.shape, Is.EqualTo(new long[] { 1, 2, 3, 256, 256 }));
    }

    [Test]
    public void ClipParityTest()
    {
        VsrUpscaleWeights weights = TestModels.TryLoadVsrUpscaleModel();
        Dictionary<string, torch.Tensor> dump = TestParity.Load("basicvsr-network");

        using BasicVsrNetwork network = weights.BuildNetwork(TestContext.CurrentContext.WorkDirectory);
        using torch.Tensor actual = Upsample(network, dump["clip"]);

        Assert.That(actual.shape, Is.EqualTo(new long[] { 1, 3, 3, 256, 256 }));
        Assert.That(TestHelpers.MaxDifference(actual, dump["output"]), Is.LessThan(1e-4f));
    }

    [Test]
    public void ChunkedTest()
    {
        torch.manual_seed(12345);

        using BasicVsrNetwork full = new(8, 1, TestContext.CurrentContext.WorkDirectory, 1024);

        torch.manual_seed(12345);

        using BasicVsrNetwork chunked = new(8, 1, TestContext.CurrentContext.WorkDirectory, 16);
        using torch.Tensor clip = torch.rand([1, 2, 3, 80, 48]);

        full.eval();
        chunked.eval();

        using torch.Tensor expected = Upsample(full, clip);
        using torch.Tensor actual = Upsample(chunked, clip);

        Assert.That(actual.shape, Is.EqualTo(new long[] { 1, 2, 3, 320, 192 }));
        Assert.That(TestHelpers.MaxDifference(actual, expected), Is.LessThan(1e-4f));
    }
}
