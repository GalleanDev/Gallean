using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra.Tests;

internal sealed class TestParameterModule : Module<torch.Tensor, torch.Tensor>
{
    private readonly Parameter alpha;

    public TestParameterModule()
        : base(nameof(TestParameterModule))
    {
        alpha = new Parameter(torch.zeros(1), requires_grad: false);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        return input.mul(alpha);
    }
}
