using TorchSharp;

namespace Umbra.CorridorKey;

internal static class CorridorKeyUnroll
{
    public static torch.Tensor Forward(torch.Tensor tokens, long gridSize, long stride, int steps)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = tokens.shape[0];
        long channels = tokens.shape[2];
        long folded = batch;
        long size = gridSize;
        torch.Tensor current = tokens.reshape(folded, size, size, channels);

        for (int step = 0; step < steps; step++)
        {
            size /= stride;

            using torch.Tensor cells = current.reshape(folded, size, stride, size, stride, channels);
            using torch.Tensor subgrids = cells.permute(0, 2, 4, 1, 3, 5);

            current = subgrids.flatten(0, 2);
            folded *= stride * stride;
        }

        return current.reshape(batch, gridSize * gridSize, channels).contiguous().MoveToOuterDisposeScope();
    }
}
