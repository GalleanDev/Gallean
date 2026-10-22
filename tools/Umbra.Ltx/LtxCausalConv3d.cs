using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxCausalConv3d : Module<torch.Tensor, torch.Tensor>
{
    public const long DefaultChunkBytes = 32L * 1024 * 1024;
    private const int KernelSize = 3;

    private readonly int outputChannels;
    private readonly long chunkBytes;

    [ComponentName(Name = "conv")]
    private readonly Conv3d convolution;

    public LtxCausalConv3d(int inputChannels, int outputChannels, bool causal, long chunkBytes)
        : base(nameof(LtxCausalConv3d))
    {
        this.outputChannels = outputChannels;
        this.chunkBytes = chunkBytes;
        Causal = causal;

        convolution = Conv3d(
            inputChannels,
            outputChannels,
            (KernelSize, KernelSize, KernelSize),
            padding: (0, KernelSize / 2, KernelSize / 2));

        RegisterComponents();
    }

    public bool Causal { get; }
    public torch.Device Device => convolution.weight.device;

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long frames = input.shape[2];
        long chunkFrames = GetChunkFrames(input);

        if (chunkFrames >= frames)
        {
            return Convolve(input, 0, frames).MoveToOuterDisposeScope();
        }

        torch.Tensor output = torch.empty(
            [input.shape[0], outputChannels, frames, input.shape[3], input.shape[4]],
            input.dtype,
            input.device);

        for (long start = 0; start < frames; start += chunkFrames)
        {
            long count = Math.Min(chunkFrames, frames - start);

            using torch.Tensor chunk = Convolve(input, start, count);
            using torch.Tensor slice = output.narrow(2, start, count);

            slice.copy_(chunk);
        }

        return output.MoveToOuterDisposeScope();
    }

    private (int Before, int After) GetHalo()
    {
        if (Causal)
        {
            return (KernelSize - 1, 0);
        }
        else
        {
            return ((KernelSize - 1) / 2, (KernelSize - 1) / 2);
        }
    }

    private long GetChunkFrames(torch.Tensor input)
    {
        long channels = Math.Max(input.shape[1], outputChannels);
        long frameBytes = input.shape[0] * channels * input.shape[3] * input.shape[4] * input.element_size();

        return Math.Max(1, chunkBytes / frameBytes);
    }

    private torch.Tensor Convolve(torch.Tensor input, long start, long count)
    {
        using IDisposable scope = torch.NewDisposeScope();

        (int before, int after) = GetHalo();
        long frames = input.shape[2];
        long available = Math.Min(start, before);
        long trailing = Math.Min(frames - (start + count), after);

        using torch.Tensor chunk = input.narrow(2, start - available, count + available + trailing);

        List<torch.Tensor> parts = [];

        if (available < before)
        {
            using torch.Tensor firstFrame = input.narrow(2, 0, 1);

            parts.Add(firstFrame.expand(-1, -1, before - available, -1, -1));
        }

        parts.Add(chunk);

        if (trailing < after)
        {
            using torch.Tensor lastFrame = input.narrow(2, frames - 1, 1);

            parts.Add(lastFrame.expand(-1, -1, after - trailing, -1, -1));
        }

        using torch.Tensor paddedChunk = torch.cat(parts, dim: 2);

        return convolution.forward(paddedChunk).MoveToOuterDisposeScope();
    }
}
