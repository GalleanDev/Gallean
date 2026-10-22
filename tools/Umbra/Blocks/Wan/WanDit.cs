using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;
using GELU = TorchSharp.Modules.GELU;

namespace Umbra;

internal sealed class WanDit : Module
{
    private readonly int dim;
    private readonly int frequencyDim;
    private readonly (long Time, long Height, long Width) patch;
    private readonly WanRope rope;

    [ComponentName(Name = "patch_embedding")]
    private readonly Conv3d patchEmbedding;

    [ComponentName(Name = "text_embedding")]
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> textEmbedding;

    [ComponentName(Name = "time_embedding")]
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> timeEmbedding;

    [ComponentName(Name = "time_projection")]
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> timeProjection;

    private readonly ModuleList<WanDitBlock> blocks;
    private readonly WanDitHead head;

    public WanDit(
        int dim,
        int inChannels,
        int outChannels,
        int feedForwardDim,
        int textDim,
        int frequencyDim,
        int heads,
        int layers,
        (long Time, long Height, long Width) patch,
        double epsilon = 1e-6)
        : base(nameof(WanDit))
    {
        this.dim = dim;
        this.frequencyDim = frequencyDim;
        this.patch = patch;

        patchEmbedding = Conv3d(inChannels, dim, kernel_size: patch, stride: patch);
        textEmbedding = new ModuleList<Module<torch.Tensor, torch.Tensor>>(
            Linear(textDim, dim), GELU(approximate: GELU.Approximate.tanh), Linear(dim, dim));
        timeEmbedding = new ModuleList<Module<torch.Tensor, torch.Tensor>>(
            Linear(frequencyDim, dim), SiLU(), Linear(dim, dim));
        timeProjection = new ModuleList<Module<torch.Tensor, torch.Tensor>>(
            SiLU(), Linear(dim, dim * 6));

        List<WanDitBlock> stack = [];

        for (int index = 0; index < layers; index++)
        {
            stack.Add(new WanDitBlock(dim, heads, feedForwardDim, epsilon));
        }

        blocks = new ModuleList<WanDitBlock>([.. stack]);
        head = new WanDitHead(dim, outChannels, patch, epsilon);
        rope = WanRope.Precompute(dim / heads);

        RegisterComponents();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            rope.Dispose();
        }

        base.Dispose(disposing);
    }

    public torch.Tensor Forward(
        torch.Tensor latents,
        torch.Tensor context,
        torch.Tensor timestep,
        TeaCache cache,
        torch.Tensor mask)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor sinusoid = WanTimestepEmbedding.Build(frequencyDim, timestep);

        using torch.Tensor time = Apply(timeEmbedding, sinusoid);
        using torch.Tensor flatModulation = Apply(timeProjection, time);
        using torch.Tensor modulation = flatModulation.unflatten(1, [6, dim]);
        using torch.Tensor textContext = Apply(textEmbedding, context);

        using torch.Tensor patches = patchEmbedding.call(latents);

        long frames = patches.shape[2];
        long height = patches.shape[3];
        long width = patches.shape[4];

        using torch.Tensor tokens = SelectTokens(mask);
        using torch.Tensor grid = rope.Build(frames, height, width, patches.device);
        using torch.Tensor frequencies = grid.index_select(0, tokens);
        using torch.Tensor patchSequence = patches.flatten(2).transpose(1, 2);

        torch.Tensor sequence = patchSequence.index_select(1, tokens);
        torch.Tensor blockOutput = cache.Apply(sequence, time, RunBlocks);

        using torch.Tensor hidden = blockOutput;
        using torch.Tensor headOutput = head.call(hidden, time);
        using torch.Tensor fullSequence = torch.zeros(
            [headOutput.shape[0], frames * height * width, headOutput.shape[2]],
            headOutput.dtype,
            headOutput.device);

        fullSequence.index_copy_(1, tokens, headOutput);

        torch.Tensor result = Unpatchify(fullSequence, frames, height, width);

        return result.MoveToOuterDisposeScope();

        torch.Tensor RunBlocks(torch.Tensor input)
        {
            torch.Tensor current = input;

            foreach (WanDitBlock block in blocks)
            {
                torch.Tensor next = block.call(current, textContext, modulation, frequencies);

                current.Dispose();
                current = next;
            }

            return current;
        }
    }

    private torch.Tensor SelectTokens(torch.Tensor mask)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long[] size = [patch.Time, patch.Height, patch.Width];

        using torch.Tensor cells = functional.max_pool3d(mask, size, size);
        using torch.Tensor cellMask = cells.reshape(-1).gt(0.0);
        using torch.Tensor indices = cellMask.nonzero();

        return indices.reshape(-1).MoveToOuterDisposeScope();
    }

    private torch.Tensor Unpatchify(torch.Tensor input, long frames, long height, long width)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long outChannels = input.shape[2] / (patch.Time * patch.Height * patch.Width);

        torch.Tensor result = input
            .reshape(input.shape[0], frames, height, width, patch.Time, patch.Height, patch.Width, outChannels)
            .permute(0, 7, 1, 4, 2, 5, 3, 6)
            .reshape(input.shape[0], outChannels, frames * patch.Time, height * patch.Height, width * patch.Width);

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Apply(ModuleList<Module<torch.Tensor, torch.Tensor>> layers, torch.Tensor input)
    {
        torch.Tensor result = input.alias();

        foreach (Module<torch.Tensor, torch.Tensor> layer in layers)
        {
            torch.Tensor next = layer.call(result);

            result.Dispose();
            result = next;
        }

        return result;
    }
}
