using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class MosrConvBlock : Module<torch.Tensor, torch.Tensor>
{
    private readonly ModuleChain block;
    private readonly Conv2d conv11;

    public MosrConvBlock(int inChannels, int outChannels)
        : base(nameof(MosrConvBlock))
    {
        block = new ModuleChain(
        [
            Conv2d(inChannels, outChannels, kernel_size: 3, stride: 1, padding: 1, bias: true),
            Mish(),
            Conv2d(outChannels, outChannels, kernel_size: 3, stride: 1, padding: 1, bias: true),
            Mish()
        ]);

        conv11 = Conv2d(inChannels, outChannels, kernel_size: 1, stride: 1, padding: 0L, bias: true);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor deep = block.call(input);
        using torch.Tensor shallow = conv11.call(input);

        torch.Tensor result = deep + shallow;

        return result.MoveToOuterDisposeScope();
    }
}
