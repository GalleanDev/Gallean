using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxVaeEncoder : Module<torch.Tensor, torch.Tensor>
{
    private const double InputPeakRatio = 8.0 / 3.0;

    private readonly int patchSize;
    private readonly int latentChannels;
    private readonly long streamBytes;
    private readonly long residentBytes;
    private readonly List<ILtxVaeStage> stages = [];

    [ComponentName(Name = "conv_in")]
    private readonly LtxCausalConv3d inputConvolution;

    [ComponentName(Name = "down_blocks")]
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> blocks;

    [ComponentName(Name = "conv_out")]
    private readonly LtxCausalConv3d outputConvolution;

    public LtxVaeEncoder(LtxVaeOptions options)
        : base(nameof(LtxVaeEncoder))
    {
        patchSize = options.PatchSize;
        latentChannels = options.LatentChannels;
        streamBytes = options.StreamBytes;
        residentBytes = options.ResidentBytes;

        int channels = options.LatentChannels;

        inputConvolution = new LtxCausalConv3d(3 * patchSize * patchSize, channels, causal: true, options.ChunkBytes);
        blocks = new ModuleList<Module<torch.Tensor, torch.Tensor>>();

        foreach (LtxVaeBlock block in options.Blocks)
        {
            if (block.Type == LtxVaeBlockType.Residual)
            {
                LtxVaeResnetStack stack = new(channels, block.Count, causal: true, options.ChunkBytes);

                blocks.Add(stack);
                stages.Add(stack);
            }
            else
            {
                int outputChannels = channels * block.Count;
                LtxVaeSpaceToDepth compress = new(channels, outputChannels, block, options.ChunkBytes);

                blocks.Add(compress);
                stages.Add(compress);
                channels = outputChannels;
            }
        }

        outputConvolution = new LtxCausalConv3d(channels, latentChannels + 1, causal: true, options.ChunkBytes);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor video)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Device device = inputConvolution.Device;
        LtxVaeConvolutionStage inputStage = new(
            inputConvolution,
            causal: true,
            frames => LtxVaeDepth.Patchify(frames, patchSize),
            features => features.alias(),
            InputPeakRatio);
        LtxVaeConvolutionStage outputStage = new(
            outputConvolution,
            causal: true,
            Activate,
            moments => moments.narrow(1, 0, latentChannels).contiguous(),
            1);

        torch.Tensor current = LtxVaeStreaming.Apply(inputStage, video, device, streamBytes, residentBytes);

        foreach (ILtxVaeStage stage in stages)
        {
            torch.Tensor next = LtxVaeStreaming.Apply(stage, current, device, streamBytes, residentBytes);

            current.Dispose();
            current = next;
        }

        using torch.Tensor hidden = current;

        using torch.Tensor means = LtxVaeStreaming.Apply(outputStage, hidden, device, streamBytes, 0);

        return means.to(device).MoveToOuterDisposeScope();
    }

    private static torch.Tensor Activate(torch.Tensor frames)
    {
        torch.Tensor result = LtxPixelNorm.Normalize(frames);

        result.silu_();

        return result;
    }
}
