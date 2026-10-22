using TorchSharp;

namespace Umbra.Ltx;

internal sealed class LtxVaeConvolutionStage(
    LtxCausalConv3d convolution,
    bool causal,
    Func<torch.Tensor, torch.Tensor> prepare,
    Func<torch.Tensor, torch.Tensor> finish,
    double peakRatio) : ILtxVaeStage
{
    private int Before => causal ? LtxVaeFrames.CausalHalo : LtxVaeFrames.SymmetricHalo;
    private int After => causal ? 0 : LtxVaeFrames.SymmetricHalo;

    public long GetOutputFrames(long inputFrames)
    {
        return inputFrames;
    }

    public long GetPeakBytes(long outputFrames, long inputFrameBytes)
    {
        return (long)((outputFrames + Before + After) * inputFrameBytes * peakRatio);
    }

    public torch.Tensor ForwardFrames(torch.Tensor input, long start, long count, torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using LtxVaeSlice slice = LtxVaeFrames.Slice(input, start - Before, count + Before + After, device);
        using torch.Tensor convolutionInput = prepare(slice.Frames);
        using torch.Tensor features = convolution.forward(convolutionInput);
        using torch.Tensor core = features.narrow(2, start - slice.Start, count);

        return finish(core).MoveToOuterDisposeScope();
    }
}
