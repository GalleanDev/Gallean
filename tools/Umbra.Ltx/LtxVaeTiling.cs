using TorchSharp;

namespace Umbra.Ltx;

internal static class LtxVaeTiling
{
    public static torch.Tensor Apply(
        Func<torch.Tensor, torch.Tensor> transform,
        torch.Tensor input,
        int inputScale,
        int outputScale,
        long size,
        long overlap)
    {
        using IDisposable scope = torch.NewDisposeScope();

        IReadOnlyList<LtxDimensionInterval> rows = LtxDimensionInterval.SplitBySize(input.shape[3] / inputScale, size, overlap);
        IReadOnlyList<LtxDimensionInterval> columns = LtxDimensionInterval.SplitBySize(input.shape[4] / inputScale, size, overlap);

        if (rows.Count > 1 || columns.Count > 1)
        {
            return Blend(transform, input, inputScale, outputScale, rows, columns).MoveToOuterDisposeScope();
        }
        else
        {
            return transform(input).MoveToOuterDisposeScope();
        }
    }

    private static torch.Tensor Blend(
        Func<torch.Tensor, torch.Tensor> transform,
        torch.Tensor input,
        int inputScale,
        int outputScale,
        IReadOnlyList<LtxDimensionInterval> rows,
        IReadOnlyList<LtxDimensionInterval> columns)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor? output = null;

        foreach (LtxDimensionInterval row in rows)
        {
            foreach (LtxDimensionInterval column in columns)
            {
                using IDisposable tileScope = torch.NewDisposeScope();

                using torch.Tensor rowSlice = input.narrow(3, row.Start * inputScale, row.Length * inputScale);
                using torch.Tensor slice = rowSlice.narrow(4, column.Start * inputScale, column.Length * inputScale);
                using torch.Tensor tile = transform(slice);

                if (output is null)
                {
                    output = torch.zeros(
                        [
                            tile.shape[0],
                            tile.shape[1],
                            tile.shape[2],
                            input.shape[3] / inputScale * outputScale,
                            input.shape[4] / inputScale * outputScale
                        ],
                        tile.dtype,
                        tile.device).MoveToOuterDisposeScope();
                }

                using torch.Tensor rowMask = ComputeTrapezoidalMask(row, outputScale, tile.dtype, tile.device).view(-1, 1);
                using torch.Tensor columnMask = ComputeTrapezoidalMask(column, outputScale, tile.dtype, tile.device).view(1, -1);
                using torch.Tensor weightedTile = tile.mul(rowMask).mul(columnMask);
                using torch.Tensor targetRows = output.narrow(3, row.Start * outputScale, row.Length * outputScale);
                using torch.Tensor target = targetRows.narrow(4, column.Start * outputScale, column.Length * outputScale);

                target.add_(weightedTile);
            }
        }

        if (output is null)
        {
            throw new InvalidOperationException("The input has no tiles.");
        }

        return output.MoveToOuterDisposeScope();
    }

    private static torch.Tensor ComputeTrapezoidalMask(
        LtxDimensionInterval interval,
        int scale,
        torch.ScalarType type,
        torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long length = interval.Length * scale;
        long leftRamp = interval.LeftRamp * scale;
        long rightRamp = interval.RightRamp * scale;
        torch.Tensor mask = torch.ones(length, torch.ScalarType.Float32, device);

        if (leftRamp > 0)
        {
            using torch.Tensor ramp = torch.linspace(0, 1, leftRamp, torch.ScalarType.Float32, device);
            using torch.Tensor head = mask.narrow(0, 0, leftRamp);

            head.copy_(ramp);
        }

        if (rightRamp > 0)
        {
            using torch.Tensor ramp = torch.linspace(1, 0, rightRamp, torch.ScalarType.Float32, device);
            using torch.Tensor tail = mask.narrow(0, length - rightRamp, rightRamp);

            tail.copy_(ramp);
        }

        return mask.to(type).MoveToOuterDisposeScope();
    }
}
