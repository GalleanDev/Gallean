using TorchSharp;

namespace Umbra.Tests;

public sealed class VsrUpscaleWeightsTests
{
    private static string FormatShape(VsrUpscaleWeights weights, string name)
    {
        torch.Tensor tensor = weights.State[name];

        return $"{name} [{string.Join(",", tensor.shape)}]";
    }

    [Test]
    public void UpscaleModelTest()
    {
        VsrUpscaleWeights weights = TestModels.TryLoadVsrUpscaleModel();

        Assert.That(weights.State, Has.Count.EqualTo(274));
        Assert.That(weights.MidChannels, Is.EqualTo(64));
        Assert.That(weights.BlockCount, Is.EqualTo(7));
    }

    [Test]
    public void RenamedKeysTest()
    {
        VsrUpscaleWeights weights = TestModels.TryLoadVsrUpscaleModel();

        Assert.That(FormatShape(weights, "upconv1.weight"), Is.EqualTo("upconv1.weight [256,64,3,3]"));
        Assert.That(FormatShape(weights, "upconv2.bias"), Is.EqualTo("upconv2.bias [256]"));
        Assert.That(
            FormatShape(weights, "spynet.basic_module.0.basic_module.0.weight"),
            Is.EqualTo("spynet.basic_module.0.basic_module.0.weight [32,8,7,7]"));
        Assert.That(
            FormatShape(weights, "spynet.basic_module.5.basic_module.8.weight"),
            Is.EqualTo("spynet.basic_module.5.basic_module.8.weight [2,16,7,7]"));
    }

    [Test]
    public void TreeShapesTest()
    {
        VsrUpscaleWeights weights = TestModels.TryLoadVsrUpscaleModel();
        List<string> rows = [];

        foreach (string name in new[]
            {
                "feat_extract.main.0.weight",
                "backbone.backward_1.main.0.weight",
                "backbone.forward_1.main.0.weight",
                "backbone.backward_2.main.0.weight",
                "backbone.forward_2.main.0.weight",
                "reconstruction.main.0.weight",
                "deform_align.backward_1.conv_offset.0.weight",
                "deform_align.backward_1.weight",
                "conv_hr.weight",
                "conv_last.weight"
            })
        {
            rows.Add(FormatShape(weights, name));
        }

        Assert.That(rows, Is.EqualTo([
            "feat_extract.main.0.weight [64,3,3,3]",
            "backbone.backward_1.main.0.weight [64,128,3,3]",
            "backbone.forward_1.main.0.weight [64,192,3,3]",
            "backbone.backward_2.main.0.weight [64,256,3,3]",
            "backbone.forward_2.main.0.weight [64,320,3,3]",
            "reconstruction.main.0.weight [64,320,3,3]",
            "deform_align.backward_1.conv_offset.0.weight [64,196,3,3]",
            "deform_align.backward_1.weight [64,128,3,3]",
            "conv_hr.weight [64,64,3,3]",
            "conv_last.weight [3,64,3,3]"
        ]));
    }
}
