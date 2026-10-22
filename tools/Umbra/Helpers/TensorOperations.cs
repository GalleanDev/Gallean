using TorchSharp;

namespace Umbra;

internal static class TensorOperations
{
    public static torch.Tensor RunChunked(
        long extent,
        long chunk,
        long dimension,
        Func<long, long, torch.Tensor> chunkFunc)
    {
        using IDisposable scope = torch.NewDisposeScope();

        if (extent > chunk)
        {
            List<torch.Tensor> chunks = [];

            for (long start = 0; start < extent; start += chunk)
            {
                long count = Math.Min(chunk, extent - start);

                chunks.Add(chunkFunc(start, count));
            }

            torch.Tensor result = torch.cat(chunks, dim: dimension);

            return result.MoveToOuterDisposeScope();
        }
        else
        {
            torch.Tensor result = chunkFunc(0, extent);

            return result.MoveToOuterDisposeScope();
        }
    }

    public static torch.Tensor RunChunked(
        torch.Tensor input,
        long chunk,
        long dimension,
        Func<torch.Tensor, torch.Tensor> chunkFunc)
    {
        return RunChunked(
            input.shape[dimension],
            chunk,
            dimension,
            (start, count) =>
            {
                using torch.Tensor slice = input.narrow(dimension, start, count);

                return chunkFunc(slice);
            });
    }

    public static torch.Tensor RunChunked(
        torch.Tensor input,
        long chunk,
        long dimension,
        long margin,
        long scale,
        Func<torch.Tensor, torch.Tensor> chunkFunc)
    {
        long extent = input.shape[dimension];

        return RunChunked(
            extent,
            chunk,
            dimension,
            (start, count) => WithMargin(input, dimension, margin, scale, extent, start, count, chunkFunc));
    }

    private static torch.Tensor WithMargin(
        torch.Tensor input,
        long dimension,
        long margin,
        long scale,
        long extent,
        long start,
        long count,
        Func<torch.Tensor, torch.Tensor> chunkFunc)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long before = Math.Min(margin, start);
        long after = Math.Min(margin, extent - start - count);

        using torch.Tensor window = input.narrow(dimension, start - before, before + count + after);
        using torch.Tensor output = chunkFunc(window);
        torch.Tensor result = output.narrow(dimension, before * scale, count * scale).clone();

        return result.MoveToOuterDisposeScope();
    }
}
