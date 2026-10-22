using TorchSharp;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxVaeDepthToSpace : Module<torch.Tensor, torch.Tensor>, ILtxVaeStage
{
    private readonly (int Time, int Height, int Width) stride;
    private readonly double peakRatio;

    [ComponentName(Name = "conv")]
    private readonly LtxCausalConv3d convolution;

    public LtxVaeDepthToSpace(int inputChannels, LtxVaeBlock block, bool causal, long chunkBytes)
        : base(nameof(LtxVaeDepthToSpace))
    {
        stride = block.Stride;

        int convolutionChannels = block.StrideProduct * inputChannels / block.Count;

        peakRatio = Math.Max(1, (double)convolutionChannels / inputChannels);
        convolution = new LtxCausalConv3d(inputChannels, convolutionChannels, causal, chunkBytes);

        RegisterComponents();
    }

    public long GetOutputFrames(long inputFrames)
    {
        if (stride.Time == 2)
        {
            return (2 * inputFrames) - 1;
        }
        else
        {
            return inputFrames;
        }
    }

    public long GetPeakBytes(long outputFrames, long inputFrameBytes)
    {
        return (long)(GetInputFrames(outputFrames) * inputFrameBytes * peakRatio);
    }

    private long GetInputFrames(long outputCount)
    {
        if (stride.Time == 2)
        {
            return ((outputCount + 1) / 2) + 3;
        }
        else
        {
            return outputCount + 2;
        }
    }

    public torch.Tensor ForwardFrames(torch.Tensor input, long start, long count, torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long firstConvolved;
        long convolvedCount;
        long offset;

        if (stride.Time == 2)
        {
            firstConvolved = (start + 1) / 2;
            convolvedCount = ((start + count) / 2) - firstConvolved + 1;
            offset = start - ((2 * firstConvolved) - 1);
        }
        else
        {
            firstConvolved = start;
            convolvedCount = count;
            offset = 0;
        }

        using LtxVaeSlice slice = LtxVaeFrames.Slice(input, firstConvolved - 1, convolvedCount + 2, device);
        using torch.Tensor features = convolution.forward(slice.Frames);
        using torch.Tensor core = features.narrow(2, firstConvolved - slice.Start, convolvedCount);
        using torch.Tensor output = LtxVaeDepth.DepthToSpace(core, stride);

        return output.narrow(2, offset, count).contiguous().MoveToOuterDisposeScope();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor features = convolution.forward(input);
        torch.Tensor output = LtxVaeDepth.DepthToSpace(features, stride);

        if (stride.Time == 2)
        {
            using torch.Tensor paddedOutput = output;

            return paddedOutput.narrow(2, 1, paddedOutput.shape[2] - 1).contiguous().MoveToOuterDisposeScope();
        }
        else
        {
            return output.MoveToOuterDisposeScope();
        }
    }
}
