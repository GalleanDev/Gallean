using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxFeedForward : Module<torch.Tensor, torch.Tensor>
{
    [ComponentName(Name = "net")]
    private readonly Sequential network;

    public LtxFeedForward(int channels, int widthRatio, bool hasBias)
        : base(nameof(LtxFeedForward))
    {
        int innerChannels = channels * widthRatio;

        network = Sequential(
            new LtxGeluProjection(channels, innerChannels, hasBias),
            Identity(),
            Linear(innerChannels, channels, hasBias));

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        return network.forward(input);
    }
}
