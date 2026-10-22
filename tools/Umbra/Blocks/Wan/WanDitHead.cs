using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class WanDitHead : Module<torch.Tensor, torch.Tensor, torch.Tensor>, IParameterModule
{
    private readonly LayerNorm norm;
    private readonly Linear head;
    private Parameter modulation;

    public WanDitHead(int dim, int outChannels, (long Time, long Height, long Width) patch, double epsilon)
        : base(nameof(WanDitHead))
    {
        norm = LayerNorm([dim], eps: epsilon, elementwise_affine: false);
        head = Linear(dim, (int)(outChannels * patch.Time * patch.Height * patch.Width));
        modulation = new Parameter(torch.zeros(1, 2, dim), requires_grad: false);

        RegisterComponents();
    }

    public void SetParameter(string name, Parameter value)
    {
        switch (name)
        {
            case "modulation":
                modulation = this.ReplaceParameter(modulation, name, value);
                break;

            default:
                throw new NotSupportedException($"The parameter '{name}' of the '{GetName()}' module is not supported.");
        }
    }

    public override torch.Tensor forward(torch.Tensor input, torch.Tensor timeModulation)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor timeModulationRows = timeModulation.unsqueeze(1);
        using torch.Tensor headModulation = modulation.to(timeModulation.dtype, timeModulation.device).add(timeModulationRows);

        torch.Tensor[] parts = headModulation.chunk(2, dim: 1);

        using torch.Tensor shift = parts[0];
        using torch.Tensor scale = parts[1];
        using torch.Tensor normalizedInput = norm.call(input);
        using torch.Tensor factor = scale.add(1.0);
        using torch.Tensor headInput = normalizedInput.mul(factor).add(shift);

        torch.Tensor result = head.call(headInput);

        return result.MoveToOuterDisposeScope();
    }
}
