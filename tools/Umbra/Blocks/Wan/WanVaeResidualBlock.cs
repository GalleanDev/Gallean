using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class WanVaeResidualBlock : Module<torch.Tensor, torch.Tensor>
{
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> residual;

    [ComponentName(Name = "shortcut")]
    private readonly WanCausalConv3d? shortcut;

    public WanVaeResidualBlock(int inChannels, int outChannels)
        : base(nameof(WanVaeResidualBlock))
    {
        residual = new ModuleList<Module<torch.Tensor, torch.Tensor>>(
            new WanRmsNorm(inChannels, images: false),
            SiLU(),
            new WanCausalConv3d(inChannels, outChannels, (3, 3, 3), (1, 1, 1)),
            new WanRmsNorm(outChannels, images: false),
            SiLU(),
            Dropout(0),
            new WanCausalConv3d(outChannels, outChannels, (3, 3, 3), (1, 1, 1)));

        shortcut = inChannels == outChannels
            ? null
            : new WanCausalConv3d(inChannels, outChannels, (1, 1, 1), (0, 0, 0));

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        return Forward(input, null);
    }

    public torch.Tensor Forward(torch.Tensor input, WanVaeCache? cache)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor identity = shortcut is null ? input.alias() : shortcut.Forward(input, null);
        torch.Tensor current = input.alias();

        foreach (Module<torch.Tensor, torch.Tensor> layer in residual)
        {
            if (layer is WanCausalConv3d conv)
            {
                torch.Tensor next = WanVaeCacheTail.Convolve(conv, current, cache);

                current.Dispose();
                current = next;
            }
            else
            {
                torch.Tensor next = layer.call(current);

                current.Dispose();
                current = next;
            }
        }

        using torch.Tensor residualOutput = current;
        using torch.Tensor shortcutOutput = identity;

        torch.Tensor result = residualOutput.add(shortcutOutput);

        return result.MoveToOuterDisposeScope();
    }
}
