using TorchSharp;

namespace Umbra.Ltx;

internal interface ILtxVaeStage
{
    long GetOutputFrames(long inputFrames);
    long GetPeakBytes(long outputFrames, long inputFrameBytes);
    torch.Tensor ForwardFrames(torch.Tensor input, long start, long count, torch.Device device);
}
