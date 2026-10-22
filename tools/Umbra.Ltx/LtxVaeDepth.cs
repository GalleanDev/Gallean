using TorchSharp;

namespace Umbra.Ltx;

internal static class LtxVaeDepth
{
    public static torch.Tensor SpaceToDepth(torch.Tensor input, (int Time, int Height, int Width) stride)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = input.shape[0];
        long channels = input.shape[1];
        long frames = input.shape[2] / stride.Time;
        long height = input.shape[3] / stride.Height;
        long width = input.shape[4] / stride.Width;

        return input
            .view(batch, channels, frames, stride.Time, height, stride.Height, width, stride.Width)
            .permute(0, 1, 3, 5, 7, 2, 4, 6)
            .reshape(batch, channels * stride.Time * stride.Height * stride.Width, frames, height, width)
            .MoveToOuterDisposeScope();
    }

    public static torch.Tensor DepthToSpace(torch.Tensor input, (int Time, int Height, int Width) stride)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = input.shape[0];
        long channels = input.shape[1] / (stride.Time * stride.Height * stride.Width);
        long frames = input.shape[2];
        long height = input.shape[3];
        long width = input.shape[4];

        return input
            .view(batch, channels, stride.Time, stride.Height, stride.Width, frames, height, width)
            .permute(0, 1, 5, 2, 6, 3, 7, 4)
            .reshape(batch, channels, frames * stride.Time, height * stride.Height, width * stride.Width)
            .MoveToOuterDisposeScope();
    }

    public static torch.Tensor Patchify(torch.Tensor input, int patchSize)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = input.shape[0];
        long channels = input.shape[1];
        long frames = input.shape[2];
        long height = input.shape[3] / patchSize;
        long width = input.shape[4] / patchSize;

        return input
            .view(batch, channels, frames, height, patchSize, width, patchSize)
            .permute(0, 1, 6, 4, 2, 3, 5)
            .reshape(batch, channels * patchSize * patchSize, frames, height, width)
            .MoveToOuterDisposeScope();
    }

    public static torch.Tensor Unpatchify(torch.Tensor input, int patchSize)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = input.shape[0];
        long channels = input.shape[1] / (patchSize * patchSize);
        long frames = input.shape[2];
        long height = input.shape[3];
        long width = input.shape[4];

        return input
            .view(batch, channels, patchSize, patchSize, frames, height, width)
            .permute(0, 1, 4, 5, 3, 6, 2)
            .reshape(batch, channels, frames, height * patchSize, width * patchSize)
            .MoveToOuterDisposeScope();
    }
}
