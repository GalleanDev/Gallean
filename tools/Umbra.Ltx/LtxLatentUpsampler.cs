using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxLatentUpsampler : Module<torch.Tensor, torch.Tensor>
{
    private const int Groups = 32;
    private const int Scale = 2;

    [ComponentName(Name = "initial_conv")]
    private readonly Conv3d initialConvolution;

    [ComponentName(Name = "initial_norm")]
    private readonly GroupNorm initialNorm;

    [ComponentName(Name = "res_blocks")]
    private readonly ModuleList<LtxUpsamplerResBlock> blocks;

    [ComponentName(Name = "upsampler")]
    private readonly Sequential upsampler;

    [ComponentName(Name = "post_upsample_res_blocks")]
    private readonly ModuleList<LtxUpsamplerResBlock> postBlocks;

    [ComponentName(Name = "final_conv")]
    private readonly Conv3d finalConvolution;

    public LtxLatentUpsampler(int latentChannels, int midChannels, int blocksPerStage)
        : base(nameof(LtxLatentUpsampler))
    {
        initialConvolution = Conv3d(latentChannels, midChannels, 3, padding: 1);
        initialNorm = GroupNorm(Groups, midChannels);
        blocks = new ModuleList<LtxUpsamplerResBlock>();
        postBlocks = new ModuleList<LtxUpsamplerResBlock>();

        for (int index = 0; index < blocksPerStage; index++)
        {
            blocks.Add(new LtxUpsamplerResBlock(midChannels));
            postBlocks.Add(new LtxUpsamplerResBlock(midChannels));
        }

        upsampler = Sequential(Conv2d(midChannels, Scale * Scale * midChannels, 3, padding: 1));
        finalConvolution = Conv3d(midChannels, latentChannels, 3, padding: 1);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor latent)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor initialHidden = initialConvolution.forward(latent);
        using torch.Tensor normalizedHidden = initialNorm.forward(initialHidden);

        torch.Tensor current = functional.silu(normalizedHidden);

        foreach (LtxUpsamplerResBlock block in blocks)
        {
            torch.Tensor next = block.forward(current);

            current.Dispose();
            current = next;
        }

        using torch.Tensor lowResolutionHidden = current;
        using torch.Tensor upsampledHidden = Upsample(lowResolutionHidden);

        current = upsampledHidden.alias();

        foreach (LtxUpsamplerResBlock block in postBlocks)
        {
            torch.Tensor next = block.forward(current);

            current.Dispose();
            current = next;
        }

        using torch.Tensor finalHidden = current;

        return finalConvolution.forward(finalHidden).MoveToOuterDisposeScope();
    }

    private torch.Tensor Upsample(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = input.shape[0];
        long channels = input.shape[1];
        long frames = input.shape[2];
        long height = input.shape[3];
        long width = input.shape[4];

        using torch.Tensor frameBatch = input.transpose(1, 2).reshape(batch * frames, channels, height, width);
        using torch.Tensor features = upsampler.forward(frameBatch);
        using torch.Tensor upsampledBatch = functional.pixel_shuffle(features, Scale);

        return upsampledBatch
            .view(batch, frames, channels, height * Scale, width * Scale)
            .transpose(1, 2)
            .contiguous()
            .MoveToOuterDisposeScope();
    }
}
