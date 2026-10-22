using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra.Tests;

public sealed class ModuleParametersTests
{
    private static IReadOnlyList<string> FormatParameters(Module module)
    {
        List<string> rows = [];

        foreach ((string name, torch.Tensor tensor) in module.state_dict())
        {
            float[] values = tensor.reshape(-1).data<float>().ToArray();

            rows.Add($"{name} = [{string.Join(", ", values)}] on {tensor.device_type}");
        }

        return rows;
    }

    [Test]
    public void ConvolutionTest()
    {
        using WanCausalConv3d convolution = MetaDevice.Construct(() => new WanCausalConv3d(1, 1, kernel: (1, 1, 1), pad: (0, 0, 0)));

        Dictionary<string, torch.Tensor> state = new()
        {
            ["weight"] = torch.full([1, 1, 1, 1, 1], 5.0f),
            ["bias"] = torch.full([1], 7.0f)
        };

        ModuleParameters.Load(convolution, state);

        Assert.That(FormatParameters(convolution), Is.EquivalentTo(["weight = [5] on CPU", "bias = [7] on CPU"]));
    }

    [Test]
    public void NormalizationTest()
    {
        using WanRmsNorm norm = MetaDevice.Construct(() => new WanRmsNorm(2, images: false));

        Dictionary<string, torch.Tensor> state = new()
        {
            ["gamma"] = torch.full([2, 1, 1, 1], 3.0f)
        };

        ModuleParameters.Load(norm, state);

        Assert.That(FormatParameters(norm), Is.EquivalentTo(["gamma = [3, 3] on CPU"]));
    }

    [Test]
    public void ConsumesStateTest()
    {
        using WanRmsNorm norm = MetaDevice.Construct(() => new WanRmsNorm(2, images: false));

        Dictionary<string, torch.Tensor> state = new()
        {
            ["gamma"] = torch.full([2, 1, 1, 1], 3.0f)
        };

        ModuleParameters.Load(norm, state);

        Assert.That(state, Is.Empty);
    }

    [Test]
    public void UnexpectedEntryTest()
    {
        using WanRmsNorm norm = MetaDevice.Construct(() => new WanRmsNorm(2, images: false));

        Dictionary<string, torch.Tensor> state = new()
        {
            ["gamma"] = torch.full([2, 1, 1, 1], 3.0f),
            ["beta"] = torch.full([2, 1, 1, 1], 4.0f)
        };

        Assert.Throws<InvalidDataException>(() => ModuleParameters.Load(norm, state));
    }

    [Test]
    public void MissingEntryTest()
    {
        using WanRmsNorm norm = MetaDevice.Construct(() => new WanRmsNorm(2, images: false));
        Dictionary<string, torch.Tensor> state = [];

        Assert.Throws<InvalidDataException>(() => ModuleParameters.Load(norm, state));
    }

    [Test]
    public void UnexpectedShapeTest()
    {
        using WanRmsNorm norm = MetaDevice.Construct(() => new WanRmsNorm(2, images: false));

        Dictionary<string, torch.Tensor> state = new()
        {
            ["gamma"] = torch.full([3, 1, 1, 1], 3.0f)
        };

        Assert.Throws<InvalidDataException>(() => ModuleParameters.Load(norm, state));
    }

    [Test]
    public void DetachesFromScopeTest()
    {
        using WanRmsNorm norm = MetaDevice.Construct(() => new WanRmsNorm(2, images: false));

        Dictionary<string, torch.Tensor> state = new()
        {
            ["gamma"] = torch.full([2, 1, 1, 1], 3.0f)
        };

        using (torch.NewDisposeScope())
        {
            ModuleParameters.Load(norm, state);
        }

        Assert.That(FormatParameters(norm), Is.EquivalentTo(["gamma = [3, 3] on CPU"]));
    }

    [Test]
    public void UnsupportedParameterTest()
    {
        using TestParameterModule module = MetaDevice.Construct(() => new TestParameterModule());

        Dictionary<string, torch.Tensor> state = new()
        {
            ["alpha"] = torch.full([1], 1.0f)
        };

        Assert.Throws<NotSupportedException>(() => ModuleParameters.Load(module, state));
    }
}
