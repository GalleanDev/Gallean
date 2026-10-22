using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class BasicVsrResidualBlock : Module<torch.Tensor, torch.Tensor>
{
    private readonly Conv2d conv1;
    private readonly Conv2d conv2;

    public BasicVsrResidualBlock(int features)
        : base(nameof(BasicVsrResidualBlock))
    {
        conv1 = Conv2d(features, features, kernel_size: 3, stride: 1, padding: 1, bias: true);
        conv2 = Conv2d(features, features, kernel_size: 3, stride: 1, padding: 1, bias: true);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor hidden = conv1.call(input);
        torch.Tensor result = input + conv2.call(functional.relu(hidden, inplace: true));

        return result.MoveToOuterDisposeScope();
    }
}
