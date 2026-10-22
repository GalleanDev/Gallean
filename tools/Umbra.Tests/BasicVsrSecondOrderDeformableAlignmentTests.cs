using TorchSharp;

namespace Umbra.Tests;

public sealed class BasicVsrSecondOrderDeformableAlignmentTests
{
    private const string AlignmentPrefix = "deform_align.backward_1.";

    private static BasicVsrSecondOrderDeformableAlignment LoadOrIgnore()
    {
        VsrUpscaleWeights weights = TestModels.TryLoadVsrUpscaleModel();
        Dictionary<string, torch.Tensor> state = [];

        foreach (KeyValuePair<string, torch.Tensor> entry in weights.State)
        {
            if (entry.Key.StartsWith(AlignmentPrefix, StringComparison.Ordinal))
            {
                state[entry.Key[AlignmentPrefix.Length..]] = entry.Value;
            }
        }

        BasicVsrSecondOrderDeformableAlignment alignment = new(weights.MidChannels, 16);

        alignment.load_state_dict(state, strict: true);
        alignment.eval();

        return alignment;
    }

    [Test]
    public void StateDictTest()
    {
        using BasicVsrSecondOrderDeformableAlignment alignment = new(64, 16);
        Dictionary<string, torch.Tensor> state = alignment.state_dict();
        List<string> rows = [];

        foreach (KeyValuePair<string, torch.Tensor> entry in state)
        {
            rows.Add($"{entry.Key} [{string.Join(",", entry.Value.shape)}]");
        }

        rows.Sort(StringComparer.Ordinal);

        Assert.That(rows, Is.EqualTo([
            "bias [64]",
            "conv_offset.0.bias [64]",
            "conv_offset.0.weight [64,196,3,3]",
            "conv_offset.2.bias [64]",
            "conv_offset.2.weight [64,64,3,3]",
            "conv_offset.4.bias [64]",
            "conv_offset.4.weight [64,64,3,3]",
            "conv_offset.6.bias [432]",
            "conv_offset.6.weight [432,64,3,3]",
            "weight [64,128,3,3]"
        ]));
    }

    [Test]
    public void AlignmentParityTest()
    {
        using BasicVsrSecondOrderDeformableAlignment alignment = LoadOrIgnore();
        Dictionary<string, torch.Tensor> dump = TestParity.Load("basicvsr-deform-align");

        using (torch.no_grad())
        {
            using torch.Tensor actual = alignment.call(
                dump["features"],
                dump["condition"],
                dump["firstFlow"],
                dump["secondFlow"]);

            Assert.That(actual.shape, Is.EqualTo(new long[] { 1, 64, 20, 16 }));
            Assert.That(TestHelpers.MaxDifference(actual, dump["output"]), Is.LessThan(1e-4f));
        }
    }
}
