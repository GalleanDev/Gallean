using TorchSharp;

namespace Umbra;

internal static class WanVaeTiling
{
    public static List<(long Start, long End)> BuildTiles(long extent, long size, long stride)
    {
        List<(long Start, long End)> spans = [];

        for (long start = 0; start < extent; start += stride)
        {
            if (start - stride < 0 || start - stride + size < extent)
            {
                spans.Add((start, Math.Min(start + size, extent)));
            }
        }

        return spans;
    }

    public static torch.Tensor BuildLineMask(
        long length,
        bool atStart,
        bool atEnd,
        long border,
        torch.ScalarType type,
        torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor weights = torch.ones(length, type, device);

        if (!atStart && border > 0)
        {
            long reach = Math.Min(border, length);
            using torch.Tensor ramp = torch.arange(1, reach + 1, type, device).div(reach);

            weights.narrow(0, 0, reach).copy_(ramp);
        }

        if (!atEnd && border > 0)
        {
            long reach = Math.Min(border, length);
            using torch.Tensor ramp = torch.arange(1, reach + 1, type, device).div(reach);
            using torch.Tensor reversedRamp = ramp.flip(0);

            weights.narrow(0, length - reach, reach).copy_(reversedRamp);
        }

        return weights.MoveToOuterDisposeScope();
    }

    public static torch.Tensor BuildMask(
        long height,
        long width,
        (bool Top, bool Bottom, bool Left, bool Right) bounds,
        (long Vertical, long Horizontal) border,
        torch.ScalarType type,
        torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor heightMask = BuildLineMask(height, bounds.Top, bounds.Bottom, border.Vertical, type, device);
        using torch.Tensor widthMask = BuildLineMask(width, bounds.Left, bounds.Right, border.Horizontal, type, device);
        using torch.Tensor rows = heightMask.reshape(height, 1).expand(height, width);
        using torch.Tensor columns = widthMask.reshape(1, width).expand(height, width);

        torch.Tensor mask = torch.minimum(rows, columns).reshape(1, 1, 1, height, width);

        return mask.MoveToOuterDisposeScope();
    }
}
