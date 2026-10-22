using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxVaeResnetStack : Module<torch.Tensor, torch.Tensor>, ILtxVaeStage
{
    private const int ConvolutionsPerBlock = 2;

    private readonly int before;
    private readonly int after;

    [ComponentName(Name = "res_blocks")]
    private readonly ModuleList<LtxVaeResnetBlock> blocks;

    public LtxVaeResnetStack(int channels, int layers, bool causal, long chunkBytes)
        : base(nameof(LtxVaeResnetStack))
    {
        if (causal)
        {
            before = layers * ConvolutionsPerBlock * LtxVaeFrames.CausalHalo;
            after = 0;
        }
        else
        {
            before = layers * ConvolutionsPerBlock * LtxVaeFrames.SymmetricHalo;
            after = before;
        }
        blocks = new ModuleList<LtxVaeResnetBlock>();

        for (int index = 0; index < layers; index++)
        {
            blocks.Add(new LtxVaeResnetBlock(channels, causal, chunkBytes));
        }

        RegisterComponents();
    }

    public long GetOutputFrames(long inputFrames)
    {
        return inputFrames;
    }

    public long GetPeakBytes(long outputFrames, long inputFrameBytes)
    {
        return (outputFrames + before + after) * inputFrameBytes;
    }

    public torch.Tensor ForwardFrames(torch.Tensor input, long start, long count, torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using LtxVaeSlice slice = LtxVaeFrames.Slice(input, start - before, count + before + after, device);
        using torch.Tensor output = forward(slice.Frames);

        return output.narrow(2, start - slice.Start, count).contiguous().MoveToOuterDisposeScope();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        torch.Tensor current = input.alias();

        foreach (LtxVaeResnetBlock block in blocks)
        {
            torch.Tensor next = block.forward(current);

            current.Dispose();
            current = next;
        }

        return current;
    }
}
