using TorchSharp;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxVaeSpaceToDepth : Module<torch.Tensor, torch.Tensor>, ILtxVaeStage
{
    private readonly (int Time, int Height, int Width) stride;

    private readonly long groupSize;
    private readonly int outputChannels;

    [ComponentName(Name = "conv")]
    private readonly LtxCausalConv3d convolution;

    public LtxVaeSpaceToDepth(int inputChannels, int outputChannels, LtxVaeBlock block, long chunkBytes)
        : base(nameof(LtxVaeSpaceToDepth))
    {
        stride = block.Stride;
        groupSize = (long)inputChannels * block.StrideProduct / outputChannels;
        this.outputChannels = outputChannels;
        convolution = new LtxCausalConv3d(inputChannels, outputChannels / block.StrideProduct, causal: true, chunkBytes);

        RegisterComponents();
    }

    public long GetOutputFrames(long inputFrames)
    {
        if (stride.Time == 2)
        {
            return (inputFrames + 1) / 2;
        }
        else
        {
            return inputFrames;
        }
    }

    public long GetPeakBytes(long outputFrames, long inputFrameBytes)
    {
        return GetInputFrames(outputFrames) * inputFrameBytes;
    }

    private long GetInputFrames(long outputCount)
    {
        if (stride.Time == 2)
        {
            return (2 * outputCount) + LtxVaeFrames.CausalHalo + 1;
        }
        else
        {
            return outputCount + LtxVaeFrames.CausalHalo;
        }
    }

    public torch.Tensor ForwardFrames(torch.Tensor input, long start, long count, torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        if (stride.Time == 2)
        {
            long grouped = 2 * count;
            long firstExtended = (2 * start) - LtxVaeFrames.CausalHalo;

            using LtxVaeSlice slice = LtxVaeFrames.Slice(input, firstExtended - 1, grouped + LtxVaeFrames.CausalHalo, device);
            using torch.Tensor extendedFrames = Extend(slice);
            using torch.Tensor features = convolution.forward(extendedFrames);

            long extendedStart = ExtendedStart(slice);

            using torch.Tensor coreInput = extendedFrames.narrow(2, (2 * start) - extendedStart, grouped);
            using torch.Tensor coreFeatures = features.narrow(2, (2 * start) - extendedStart, grouped);

            return Combine(coreInput, coreFeatures).MoveToOuterDisposeScope();
        }
        else
        {
            using LtxVaeSlice slice = LtxVaeFrames.Slice(input, start - LtxVaeFrames.CausalHalo, count + LtxVaeFrames.CausalHalo, device);
            using torch.Tensor features = convolution.forward(slice.Frames);
            using torch.Tensor coreInput = slice.Frames.narrow(2, start - slice.Start, count);
            using torch.Tensor coreFeatures = features.narrow(2, start - slice.Start, count);

            return Combine(coreInput, coreFeatures).MoveToOuterDisposeScope();
        }
    }

    private static torch.Tensor Extend(LtxVaeSlice slice)
    {
        if (slice.Start == 0)
        {
            using torch.Tensor firstFrame = slice.Frames.narrow(2, 0, 1);

            return torch.cat([firstFrame, slice.Frames], dim: 2);
        }
        else
        {
            return slice.Frames.alias();
        }
    }

    private static long ExtendedStart(LtxVaeSlice slice)
    {
        if (slice.Start == 0)
        {
            return 0;
        }
        else
        {
            return slice.Start + 1;
        }
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor extendedFrames;

        if (stride.Time == 2)
        {
            using torch.Tensor firstFrame = input.narrow(2, 0, 1);

            extendedFrames = torch.cat([firstFrame, input], dim: 2);
        }
        else
        {
            extendedFrames = input.alias();
        }

        using torch.Tensor owned = extendedFrames;
        using torch.Tensor features = convolution.forward(owned);

        return Combine(owned, features).MoveToOuterDisposeScope();
    }

    private torch.Tensor Combine(torch.Tensor input, torch.Tensor features)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor depthInput = LtxVaeDepth.SpaceToDepth(input, stride);
        using torch.Tensor skip = depthInput
            .view(depthInput.shape[0], outputChannels, groupSize, depthInput.shape[2], depthInput.shape[3], depthInput.shape[4])
            .mean([2]);
        using torch.Tensor depthFeatures = LtxVaeDepth.SpaceToDepth(features, stride);

        return depthFeatures.add(skip).MoveToOuterDisposeScope();
    }
}
