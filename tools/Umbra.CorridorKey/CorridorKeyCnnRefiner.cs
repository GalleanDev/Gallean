using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.CorridorKey;

internal sealed class CorridorKeyCnnRefiner : Module<torch.Tensor, torch.Tensor, torch.Tensor>
{
    [ComponentName(Name = "stem")]
    private readonly Sequential stem;

    [ComponentName(Name = "res1")]
    private readonly CorridorKeyRefinerBlock first;

    [ComponentName(Name = "res2")]
    private readonly CorridorKeyRefinerBlock second;

    [ComponentName(Name = "res3")]
    private readonly CorridorKeyRefinerBlock third;

    [ComponentName(Name = "res4")]
    private readonly CorridorKeyRefinerBlock fourth;

    [ComponentName(Name = "final")]
    private readonly Conv2d output;

    public CorridorKeyCnnRefiner(int inputChannels, int hiddenChannels, int outputChannels)
        : base(nameof(CorridorKeyCnnRefiner))
    {
        stem = Sequential(
            Conv2d(inputChannels, hiddenChannels, kernel_size: 3, padding: 1, bias: true),
            GroupNorm(8, hiddenChannels),
            ReLU());
        first = new CorridorKeyRefinerBlock(hiddenChannels, dilation: 1);
        second = new CorridorKeyRefinerBlock(hiddenChannels, dilation: 2);
        third = new CorridorKeyRefinerBlock(hiddenChannels, dilation: 4);
        fourth = new CorridorKeyRefinerBlock(hiddenChannels, dilation: 8);
        output = Conv2d(hiddenChannels, outputChannels, kernel_size: 1, bias: true);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor colors, torch.Tensor coarse)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor input = torch.cat([colors, coarse], dim: 1);
        using torch.Tensor stemOutput = stem.call(input);
        using torch.Tensor firstStage = first.call(stemOutput);
        using torch.Tensor secondStage = second.call(firstStage);
        using torch.Tensor thirdStage = third.call(secondStage);
        using torch.Tensor fourthStage = fourth.call(thirdStage);
        using torch.Tensor finalOutput = output.call(fourthStage);

        return finalOutput.mul(10.0).MoveToOuterDisposeScope();
    }
}
