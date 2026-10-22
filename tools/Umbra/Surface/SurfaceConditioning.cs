using TorchSharp;

namespace Umbra;

internal sealed record SurfaceConditioning(torch.Tensor Encoded, long FrameCount, torch.Tensor Mask);
