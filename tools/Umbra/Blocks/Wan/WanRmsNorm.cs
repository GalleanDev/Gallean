using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class WanRmsNorm : Module<torch.Tensor, torch.Tensor>, IParameterModule
{
    private readonly double scale;
    private readonly bool channelFirst;
    private Parameter gamma;
    private Parameter? bias;

    public WanRmsNorm(int channels, bool channelFirst = true, bool images = true, bool withBias = false)
        : base(nameof(WanRmsNorm))
    {
        this.channelFirst = channelFirst;
        scale = Math.Sqrt(channels);

        long[] shape = channelFirst switch
        {
            true when images => [channels, 1, 1],
            true => [channels, 1, 1, 1],
            false => [channels]
        };

        gamma = new Parameter(torch.ones(shape), requires_grad: false);
        bias = withBias ? new Parameter(torch.zeros(shape), requires_grad: false) : null;

        RegisterComponents();
    }

    public void SetParameter(string name, Parameter value)
    {
        switch (name)
        {
            case "gamma":
                gamma = this.ReplaceParameter(gamma, name, value);
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
        using IDisposable scope = torch.NewDisposeScope();

        long dimension = channelFirst ? 1 : input.dim() - 1;

        using torch.Tensor normalizedInput = functional.normalize(input, dim: dimension);
        using torch.Tensor scaledInput = normalizedInput.mul(scale).mul(gamma);

        torch.Tensor result = bias is null ? scaledInput.alias() : scaledInput.add(bias);

        return result.MoveToOuterDisposeScope();
    }
}
