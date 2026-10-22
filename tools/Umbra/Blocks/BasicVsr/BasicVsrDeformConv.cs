using TorchSharp;

namespace Umbra;

internal static partial class BasicVsrDeformConv
{
    private const long MaxColumnSize = 64L * 1024 * 1024;

    public static torch.Tensor Apply(
        torch.Tensor input,
        torch.Tensor offset,
        torch.Tensor mask,
        torch.Tensor weight,
        torch.Tensor bias,
        int padding,
        int dilation)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long channels = input.shape[1];
        long outputChannels = weight.shape[0];
        long kernelHeight = weight.shape[2];
        long kernelWidth = weight.shape[3];
        long taps = kernelHeight * kernelWidth;
        long groups = offset.shape[1] / (2 * taps);

        Layout layout = new(
            input.shape[0],
            channels,
            groups,
            channels / groups,
            taps,
            kernelHeight,
            kernelWidth,
            input.shape[2],
            input.shape[3],
            padding,
            dilation);

        torch.Tensor packedWeight = PackWeight(weight, outputChannels, layout);

        torch.Tensor output = TensorOperations.RunChunked(
            layout.Height,
            ChunkRows(layout),
            2,
            (start, count) => ApplyRows(input, offset, mask, packedWeight, bias, layout, start, count));

        return output.MoveToOuterDisposeScope();
    }

    private static long ChunkRows(Layout layout)
    {
        return Math.Max(1, MaxColumnSize / (layout.Channels * layout.Taps * layout.Width));
    }

    private static torch.Tensor ApplyRows(
        torch.Tensor input,
        torch.Tensor offset,
        torch.Tensor mask,
        torch.Tensor packedWeight,
        torch.Tensor bias,
        Layout layout,
        long rowStart,
        long rowCount)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor packedColumns = PackColumns(input, offset, mask, layout, rowStart, rowCount);
        torch.Tensor result = torch.nn.functional.conv2d(packedColumns, packedWeight, bias);

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor BuildGrid(
        torch.Tensor offset,
        torch.ScalarType dtype,
        Layout layout,
        long rowStart,
        long rowCount)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor rowRange = torch.arange(rowCount, dtype);
        torch.Tensor rowIndices = rowRange.add(rowStart);
        torch.Tensor rows = rowIndices.reshape(1, rowCount, 1);
        torch.Tensor columns = torch.arange(layout.Width, dtype).reshape(1, 1, layout.Width);
        torch.Tensor kernelRows = torch.arange(layout.KernelHeight, dtype)
            .reshape(layout.KernelHeight, 1, 1, 1).mul(layout.Dilation).sub(layout.Padding);
        torch.Tensor kernelColumns = torch.arange(layout.KernelWidth, dtype)
            .reshape(1, layout.KernelWidth, 1, 1).mul(layout.Dilation).sub(layout.Padding);

        torch.Tensor offsets = offset
            .view(layout.Batch, layout.Groups, layout.Taps, 2, layout.Height, layout.Width)
            .narrow(4, rowStart, rowCount);

        torch.Tensor sampleRows = (rows + kernelRows)
            .expand(layout.KernelHeight, layout.KernelWidth, rowCount, layout.Width)
            .reshape(1, 1, layout.Taps, rowCount, layout.Width) + offsets.select(3, 0);

        torch.Tensor sampleColumns = (columns + kernelColumns)
            .expand(layout.KernelHeight, layout.KernelWidth, rowCount, layout.Width)
            .reshape(1, 1, layout.Taps, rowCount, layout.Width) + offsets.select(3, 1);

        torch.Tensor grid = torch.stack(
            [
                sampleColumns.mul(2.0).div(Math.Max(layout.Width - 1, 1)).sub(1.0),
                sampleRows.mul(2.0).div(Math.Max(layout.Height - 1, 1)).sub(1.0)
            ],
            dim: -1).reshape(layout.Batch * layout.Groups, layout.Taps * rowCount, layout.Width, 2);

        return grid.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Sample(
        torch.Tensor input,
        torch.Tensor offset,
        Layout layout,
        long rowStart,
        long rowCount)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor grid = BuildGrid(offset, input.dtype, layout, rowStart, rowCount);
        torch.Tensor samples = torch.nn.functional.grid_sample(
            input.reshape(layout.Batch * layout.Groups, layout.GroupChannels, layout.Height, layout.Width),
            grid,
            align_corners: true);

        return samples.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Modulate(
        torch.Tensor input,
        torch.Tensor offset,
        torch.Tensor mask,
        Layout layout,
        long rowStart,
        long rowCount)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor samples = Sample(input, offset, layout, rowStart, rowCount);
        torch.Tensor masks = mask
            .view(layout.Batch, layout.Groups, 1, layout.Taps, layout.Height, layout.Width)
            .narrow(4, rowStart, rowCount);
        torch.Tensor modulatedSamples = samples
            .view(layout.Batch, layout.Groups, layout.GroupChannels, layout.Taps, rowCount, layout.Width)
            .mul(masks);

        return modulatedSamples.MoveToOuterDisposeScope();
    }

    private static torch.Tensor PackColumns(
        torch.Tensor input,
        torch.Tensor offset,
        torch.Tensor mask,
        Layout layout,
        long rowStart,
        long rowCount)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor modulatedSamples = Modulate(input, offset, mask, layout, rowStart, rowCount);
        torch.Tensor columns = modulatedSamples
            .permute(0, 1, 3, 2, 4, 5)
            .reshape(layout.Batch, layout.Channels * layout.Taps, rowCount, layout.Width);

        return columns.MoveToOuterDisposeScope();
    }

    private static torch.Tensor PackWeight(torch.Tensor weight, long outputChannels, Layout layout)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor packedWeight = weight
            .view(outputChannels, layout.Groups, layout.GroupChannels, layout.Taps)
            .permute(0, 1, 3, 2)
            .reshape(outputChannels, layout.Channels * layout.Taps, 1, 1);

        return packedWeight.MoveToOuterDisposeScope();
    }
}
