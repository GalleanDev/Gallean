using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class BasicVsrConvResidualBlocks : Module<torch.Tensor, torch.Tensor>
{
    private readonly ModuleChain main;

    public BasicVsrConvResidualBlocks(int inputChannels, int features, int blockCount)
        : base(nameof(BasicVsrConvResidualBlocks))
    {
        List<Module<torch.Tensor, torch.Tensor>> blocks = [];

        for (int index = 0; index < blockCount; index++)
        {
            blocks.Add(new BasicVsrResidualBlock(features));
        }

        main = new ModuleChain(
        [
            Conv2d(inputChannels, features, kernel_size: 3, stride: 1, padding: 1, bias: true),
            LeakyReLU(0.1, inplace: true),
            new ModuleChain(blocks)
        ]);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor result = main.call(input);

        return result.MoveToOuterDisposeScope();
    }
}
