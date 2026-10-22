using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.CorridorKey;

internal sealed class CorridorKeyBatchNorm : Module<torch.Tensor, torch.Tensor>, IParameterModule
{
    private const double Epsilon = 1e-5;

    [ComponentName(Name = "weight")]
    private Parameter weight;

    [ComponentName(Name = "bias")]
    private Parameter bias;

    [ComponentName(Name = "running_mean")]
    private Parameter mean;

    [ComponentName(Name = "running_var")]
    private Parameter variance;

    public CorridorKeyBatchNorm(int channels)
        : base(nameof(CorridorKeyBatchNorm))
    {
        weight = new Parameter(torch.ones(channels), requires_grad: false);
        bias = new Parameter(torch.zeros(channels), requires_grad: false);
        mean = new Parameter(torch.zeros(channels), requires_grad: false);
        variance = new Parameter(torch.ones(channels), requires_grad: false);

        RegisterComponents();
    }

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

            case "running_mean":
                mean = this.ReplaceParameter(mean, name, value);
                break;

            case "running_var":
                variance = this.ReplaceParameter(variance, name, value);
                break;

            default:
                throw new NotSupportedException($"The parameter '{name}' of the '{GetName()}' module is not supported.");
        }
    }

    public override torch.Tensor forward(torch.Tensor planes)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor inverseDeviation = variance.add(Epsilon).rsqrt();
        using torch.Tensor scale = inverseDeviation.mul(weight);
        using torch.Tensor shift = bias.sub(mean.mul(scale));
        using torch.Tensor broadcastScale = scale.view(1, -1, 1, 1);
        using torch.Tensor broadcastShift = shift.view(1, -1, 1, 1);

        return planes.mul(broadcastScale).add(broadcastShift).MoveToOuterDisposeScope();
    }
}
