using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class MosrNetwork : Module<torch.Tensor, torch.Tensor>
{
    private const double ShortcutOffset = 0.5;

    private readonly ModuleChain gblocks;
    private readonly MosrConvBlock shortcut;
    private readonly MosrDySample upsampler;

    public MosrNetwork(MosrOptions options)
        : base(nameof(MosrNetwork))
    {
        List<Module<torch.Tensor, torch.Tensor>> stages =
        [
            Conv2d(options.InChannels, options.Dimension, kernel_size: 3, stride: 1, padding: 1, bias: true)
        ];

        for (int index = 0; index < options.BlockCount; index++)
        {
            stages.Add(new MosrGatedCnnBlock(
                options.Dimension, options.ExpansionRatio, options.ConvRatio, options.KernelSize));
        }

        stages.Add(Conv2d(options.Dimension, options.Dimension * 2, kernel_size: 3, stride: 1, padding: 1, bias: true));
        stages.Add(Mish());
        stages.Add(Conv2d(options.Dimension * 2, options.Dimension, kernel_size: 3, stride: 1, padding: 1, bias: true));
        stages.Add(Mish());
        stages.Add(Conv2d(options.Dimension, options.Dimension, kernel_size: 1, stride: 1, padding: 0L, bias: true));

        gblocks = new ModuleChain(stages);
        shortcut = new MosrConvBlock(options.InChannels, options.Dimension);
        upsampler = new MosrDySample(options.Dimension, options.OutChannels, options.Scale, options.SampleGroups);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor deep = gblocks.call(input);
        using torch.Tensor shallow = shortcut.call(input);
        using torch.Tensor offsetShallow = shallow - ShortcutOffset;
        using torch.Tensor features = deep + offsetShallow;

        torch.Tensor result = upsampler.call(features);

        return result.MoveToOuterDisposeScope();
    }
}
