using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxVaeDecoder : Module<torch.Tensor, torch.Tensor>
{
    private readonly int patchSize;
    private readonly long streamBytes;
    private readonly long residentBytes;
    private readonly double inputPeakRatio;
    private readonly List<ILtxVaeStage> stages = [];

    [ComponentName(Name = "conv_in")]
    private readonly LtxCausalConv3d inputConvolution;

    [ComponentName(Name = "up_blocks")]
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> blocks;

    [ComponentName(Name = "conv_out")]
    private readonly LtxCausalConv3d outputConvolution;

    public LtxVaeDecoder(LtxVaeOptions options, bool causal)
        : base(nameof(LtxVaeDecoder))
    {
        patchSize = options.PatchSize;
        streamBytes = options.StreamBytes;
        residentBytes = options.ResidentBytes;

        int channels = options.DecoderBottleneckChannels;

        inputPeakRatio = (double)channels / options.LatentChannels;

        inputConvolution = new LtxCausalConv3d(options.LatentChannels, channels, causal, options.ChunkBytes);
        blocks = new ModuleList<Module<torch.Tensor, torch.Tensor>>();

        for (int index = options.DecoderBlocks.Count - 1; index >= 0; index--)
        {
            LtxVaeBlock block = options.DecoderBlocks[index];

            if (block.Type == LtxVaeBlockType.Residual)
            {
                LtxVaeResnetStack stack = new(channels, block.Count, causal, options.ChunkBytes);

                blocks.Add(stack);
                stages.Add(stack);
            }
            else
            {
                LtxVaeDepthToSpace expand = new(channels, block, causal, options.ChunkBytes);

                blocks.Add(expand);
                stages.Add(expand);
                channels /= block.Count;
            }
        }

        outputConvolution = new LtxCausalConv3d(channels, 3 * patchSize * patchSize, causal, options.ChunkBytes);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor latent)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Device device = inputConvolution.Device;
        bool causal = inputConvolution.Causal;
        LtxVaeConvolutionStage inputStage = new(
            inputConvolution,
            causal,
            frames => frames.alias(),
            features => features.alias(),
            inputPeakRatio);
        LtxVaeConvolutionStage outputStage = new(
            outputConvolution,
            causal,
            Activate,
            patches => LtxVaeDepth.Unpatchify(patches, patchSize),
            1);

        torch.Tensor current = LtxVaeStreaming.Apply(inputStage, latent, device, streamBytes, residentBytes);

        foreach (ILtxVaeStage stage in stages)
        {
            torch.Tensor next = LtxVaeStreaming.Apply(stage, current, device, streamBytes, residentBytes);

            current.Dispose();
            current = next;
        }

        using torch.Tensor hidden = current;

        return LtxVaeStreaming.Apply(outputStage, hidden, device, streamBytes, residentBytes).MoveToOuterDisposeScope();
    }

    private static torch.Tensor Activate(torch.Tensor frames)
    {
        torch.Tensor result = LtxPixelNorm.Normalize(frames);

        result.silu_();

        return result;
    }
}
