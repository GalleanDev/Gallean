using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class WanCausalConv3d : Module<torch.Tensor, torch.Tensor>, IParameterModule
{
    private readonly long[] padding;
    private readonly PaddingModes paddingMode;
    private readonly long[] stride;

    private Parameter weight;
    private Parameter bias;

    public WanCausalConv3d(
        int inChannels,
        int outChannels,
        (long Time, long Height, long Width) kernel,
        (long Time, long Height, long Width) pad,
        (long Time, long Height, long Width)? stride = null,
        PaddingModes mode = PaddingModes.Constant)
        : base(nameof(WanCausalConv3d))
    {
        padding = [pad.Width, pad.Width, pad.Height, pad.Height, 2 * pad.Time, 0];
        paddingMode = mode;

        (long Time, long Height, long Width) steps = stride ?? (1, 1, 1);

        this.stride = [steps.Time, steps.Height, steps.Width];

        weight = new Parameter(
            torch.zeros([outChannels, inChannels, kernel.Time, kernel.Height, kernel.Width]), requires_grad: false);
        bias = new Parameter(torch.zeros([outChannels]), requires_grad: false);

        RegisterComponents();
    }

    public torch.ScalarType Dtype => weight.dtype;

    public void SetParameter(string name, Parameter value)
    {
        switch (name)
        {
            case "weight":
                weight = this.ReplaceParameter(weight, name, value);
                break;
            case "bias":
                bias = this.ReplaceParameter(bias, name, value);
                break;

            default:
                throw new NotSupportedException($"The parameter '{name}' of the '{GetName()}' module is not supported.");
        }
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        return Forward(input, null);
    }

    public torch.Tensor Forward(torch.Tensor input, torch.Tensor? cache)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor source = input;
        long[] applied = [.. padding];

        if (cache is not null && padding[4] > 0)
        {
            source = torch.cat([cache.to(input.dtype, input.device), input], dim: 2);
            applied[4] -= cache.shape[2];
        }

        using torch.Tensor paddedSource = functional.pad(source, applied, paddingMode);

        torch.Tensor result = functional.conv3d(paddedSource, weight, bias, strides: stride);

        return result.MoveToOuterDisposeScope();
    }
}
