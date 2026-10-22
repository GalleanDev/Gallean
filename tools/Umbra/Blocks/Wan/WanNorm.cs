using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class WanNorm : Module<torch.Tensor, torch.Tensor>, IParameterModule
{
    private readonly double epsilon;
    private Parameter weight;

    public WanNorm(int dim, double epsilon = 1e-5)
        : base(nameof(WanNorm))
    {
        this.epsilon = epsilon;

        weight = new Parameter(torch.ones(dim), requires_grad: false);

        RegisterComponents();
    }

    public void SetParameter(string name, Parameter value)
    {
        switch (name)
        {
            case "weight":
                weight = this.ReplaceParameter(weight, name, value);
                break;

            default:
                throw new NotSupportedException($"The parameter '{name}' of the '{GetName()}' module is not supported.");
        }
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor floatInput = input.to(torch.float32);
        using torch.Tensor meanSquare = floatInput.pow(2).mean([-1], keepdim: true);
        using torch.Tensor inverse = meanSquare.add(epsilon).rsqrt();
        using torch.Tensor normalizedInput = floatInput.mul(inverse).to(input.dtype);

        torch.Tensor result = normalizedInput.mul(weight);

        return result.MoveToOuterDisposeScope();
    }
}
