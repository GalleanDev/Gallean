using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra;

internal static class SurfaceBackground
{
    private const long MaxChunkSize = 8L * 1024 * 1024;
    private const double Epsilon = 1e-4;
    private const double ClampFloor = 0.1;
    private const int FillIterations = 16;
    private const int DilateIterations = 2;

    public static torch.Tensor Remove(torch.Tensor field, torch.Tensor coverage)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long frames = field.shape[0];
        long chunk = ChunkFrames(field);
        List<torch.Tensor> chunks = [];

        for (long start = 0; start < frames; start += chunk)
        {
            long count = Math.Min(chunk, frames - start);

            chunks.Add(RemoveFrames(field, coverage, start, count));
        }

        torch.Tensor result = torch.cat(chunks, dim: 0);

        return result.MoveToOuterDisposeScope();
    }

    private static long ChunkFrames(torch.Tensor values)
    {
        return Math.Max(1, MaxChunkSize / (values.shape[1] * values.shape[2] * values.shape[3]));
    }

    private static torch.Tensor RemoveFrames(
        torch.Tensor field,
        torch.Tensor coverage,
        long frameStart,
        long frameCount)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor fieldChunk = field.narrow(0, frameStart, frameCount);
        using torch.Tensor coverageChunk = coverage.narrow(0, frameStart, frameCount);

        using torch.Tensor backgroundMask = CleanOutside(coverageChunk);
        using torch.Tensor background = Fill(fieldChunk, backgroundMask);
        using torch.Tensor spill = background.mul(coverageChunk.neg().add(1.0));
        using torch.Tensor foreground = fieldChunk.sub(spill);

        torch.Tensor result = foreground.div(coverageChunk.clamp_min(ClampFloor)).clamp(0.0, 1.0);

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor CleanOutside(torch.Tensor coverage)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor mask = coverage.gt(0.0).to(coverage.dtype);
        using torch.Tensor dilatedMask = Dilate(mask, DilateIterations);

        return dilatedMask.neg().add(1.0).MoveToOuterDisposeScope();
    }

    private static torch.Tensor Dilate(torch.Tensor mask, int iterations)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor result = mask.alias();

        for (int iteration = 0; iteration < iterations; iteration++)
        {
            torch.Tensor next = functional.max_pool2d(result, kernel_size: 3, stride: 1, padding: 1);

            result.Dispose();
            result = next;
        }

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Fill(torch.Tensor values, torch.Tensor known)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor estimate = values.mul(known);
        torch.Tensor weight = known.alias();

        for (int iteration = 0; iteration < FillIterations; iteration++)
        {
            using torch.Tensor spreadValues = functional.avg_pool2d(estimate, kernel_size: 3, stride: 1, padding: 1);
            using torch.Tensor spreadWeight = functional.avg_pool2d(weight, kernel_size: 3, stride: 1, padding: 1);
            using torch.Tensor spreadAverage = spreadValues.div(spreadWeight.clamp_min(Epsilon));
            using torch.Tensor reachedMask = spreadWeight.gt(0.0).to(weight.dtype);

            torch.Tensor nextEstimate = torch.where(weight.gt(0.0).expand_as(estimate), estimate, spreadAverage.mul(reachedMask));
            torch.Tensor nextWeight = torch.maximum(weight, reachedMask);

            estimate.Dispose();
            weight.Dispose();
            estimate = nextEstimate;
            weight = nextWeight;
        }

        return estimate.MoveToOuterDisposeScope();
    }
}
