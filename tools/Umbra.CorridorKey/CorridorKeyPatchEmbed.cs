using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.CorridorKey;

internal sealed class CorridorKeyPatchEmbed : Module<torch.Tensor, torch.Tensor>
{
    [ComponentName(Name = "proj")]
    private readonly Conv2d projection;

    public CorridorKeyPatchEmbed(int inputChannels, int outputChannels, int kernel, int stride, int padding)
        : base(nameof(CorridorKeyPatchEmbed))
    {
        projection = Conv2d(inputChannels, outputChannels, kernel_size: kernel, stride: stride, padding: padding, bias: true);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor planes = projection.call(input);

        return planes.reshape(planes.shape[0], planes.shape[1], -1).transpose(2, 1).contiguous().MoveToOuterDisposeScope();
    }
}
