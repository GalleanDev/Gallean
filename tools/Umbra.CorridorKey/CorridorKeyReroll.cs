using TorchSharp;

namespace Umbra.CorridorKey;

internal static class CorridorKeyReroll
{
    public static torch.Tensor Forward(torch.Tensor tokens, long gridSize, long stride, int steps)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = tokens.shape[0];
        long count = tokens.shape[1];
        long channels = tokens.shape[2];
        long unitSize = 1;
        torch.Tensor current = tokens;

        for (int step = 0; step < steps; step++)
        {
            long remaining = count / (stride * stride);

            using torch.Tensor subgrids = current.reshape(batch, stride, stride, remaining, unitSize, unitSize, channels);
            using torch.Tensor cells = subgrids.permute(0, 3, 1, 4, 2, 5, 6);

            unitSize *= stride;
            count = remaining;
            current = cells.reshape(batch, count, unitSize, unitSize, channels);
        }

        using torch.Tensor windows = current.reshape(batch, count, unitSize, unitSize, channels);

        return UndoWindowing(windows, gridSize, unitSize).MoveToOuterDisposeScope();
    }

    private static torch.Tensor UndoWindowing(torch.Tensor windows, long gridSize, long unitSize)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = windows.shape[0];
        long channels = windows.shape[^1];
        long units = gridSize / unitSize;

        return windows
            .reshape(batch, units, units, unitSize, unitSize, channels)
            .permute(0, 1, 3, 2, 4, 5)
            .reshape(batch, gridSize, gridSize, channels)
            .MoveToOuterDisposeScope();
    }
}
