using TorchSharp;

namespace Umbra.Tests;

public sealed class BasicVsrSpyNetTests
{
    private const string SpyNetPrefix = "spynet.";

    private static BasicVsrSpyNet LoadOrIgnore()
    {
        VsrUpscaleWeights weights = TestModels.TryLoadVsrUpscaleModel();
        Dictionary<string, torch.Tensor> state = [];

        foreach (KeyValuePair<string, torch.Tensor> entry in weights.State)
        {
            if (entry.Key.StartsWith(SpyNetPrefix, StringComparison.Ordinal))
            {
                state[entry.Key[SpyNetPrefix.Length..]] = entry.Value;
            }
        }

        BasicVsrSpyNet spynet = new();

        spynet.load_state_dict(state, strict: true);
        spynet.eval();

        return spynet;
    }

    [Test]
    public void StateDictTest()
    {
        using BasicVsrSpyNet spynet = new();
        Dictionary<string, torch.Tensor> state = spynet.state_dict();
        List<string> names = [.. state.Keys];

        names.Sort(StringComparer.Ordinal);

        Assert.That(state, Has.Count.EqualTo(62));
        Assert.That(names[0], Is.EqualTo("basic_module.0.basic_module.0.bias"));
        Assert.That(names[^1], Is.EqualTo("std"));
        Assert.That(state["basic_module.5.basic_module.8.weight"].shape, Is.EqualTo(new long[] { 2, 16, 7, 7 }));
        Assert.That(state["mean"].shape, Is.EqualTo(new long[] { 1, 3, 1, 1 }));
    }

    [Test]
    public void BasicModuleParityTest()
    {
        using BasicVsrSpyNet spynet = LoadOrIgnore();
        Dictionary<string, torch.Tensor> dump = TestParity.Load("basicvsr-spynet-block");

        torch.nn.Module blocks = spynet.named_children().First(child => child.name == "basic_module").module;
        torch.nn.Module block = blocks.named_children().First(child => child.name == "0").module;
        torch.nn.Module<torch.Tensor, torch.Tensor> typed = (torch.nn.Module<torch.Tensor, torch.Tensor>)block;

        using (torch.no_grad())
        {
            using torch.Tensor actual = typed.call(dump["blockInput"]);

            Assert.That(TestHelpers.MaxDifference(actual, dump["blockOutput"]), Is.LessThan(1e-4f));
        }
    }

    [Test]
    public void FlowWarpParityTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("basicvsr-flow-warp");
        torch.Tensor planes = dump["warpPlanes"];
        torch.Tensor flow = dump["warpFlow"];

        using (torch.no_grad())
        {
            using torch.Tensor zeros = BasicVsrFlow.Warp(planes, flow, torch.GridSamplePaddingMode.Zeros);
            using torch.Tensor border = BasicVsrFlow.Warp(planes, flow, torch.GridSamplePaddingMode.Border);

            Assert.That(TestHelpers.MaxDifference(zeros, dump["warpedZeros"]), Is.LessThan(1e-4f));
            Assert.That(TestHelpers.MaxDifference(border, dump["warpedBorder"]), Is.LessThan(1e-4f));
        }
    }

    [Test]
    public void FlowParityTest()
    {
        using BasicVsrSpyNet spynet = LoadOrIgnore();
        Dictionary<string, torch.Tensor> dump = TestParity.Load("basicvsr-spynet");

        using (torch.no_grad())
        {
            using torch.Tensor actual = spynet.call(
                dump["reference"], dump["supporting"]);

            Assert.That(actual.shape, Is.EqualTo(new long[] { 1, 2, 64, 64 }));
            Assert.That(TestHelpers.MaxDifference(actual, dump["flow"]), Is.LessThan(1e-4f));
        }
    }
}
