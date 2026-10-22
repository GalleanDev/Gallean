using TorchSharp;

namespace Umbra.Ltx;

internal static class LtxPositions
{
    public static torch.Tensor Build(
        long frames,
        long height,
        long width,
        int temporalFactor,
        int spatialFactor,
        int positionScale,
        double framesPerSecond,
        torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor frameIndex = torch.arange(frames, torch.ScalarType.Float32, device).view(frames, 1, 1);
        using torch.Tensor rowIndex = torch.arange(height, torch.ScalarType.Float32, device).view(1, height, 1);
        using torch.Tensor columnIndex = torch.arange(width, torch.ScalarType.Float32, device).view(1, 1, width);
        using torch.Tensor frameStart = frameIndex.expand(frames, height, width).reshape(-1);
        using torch.Tensor rowStart = rowIndex.expand(frames, height, width).reshape(-1);
        using torch.Tensor columnStart = columnIndex.expand(frames, height, width).reshape(-1);
        using torch.Tensor timeStart = ScaleTime(frameStart, temporalFactor, framesPerSecond);
        using torch.Tensor timeEnd = ScaleTime(frameStart.add(1.0), temporalFactor, framesPerSecond);
        using torch.Tensor heightStart = rowStart.mul(spatialFactor * positionScale);
        using torch.Tensor heightEnd = rowStart.add(1.0).mul(spatialFactor * positionScale);
        using torch.Tensor widthStart = columnStart.mul(spatialFactor * positionScale);
        using torch.Tensor widthEnd = columnStart.add(1.0).mul(spatialFactor * positionScale);
        using torch.Tensor starts = torch.stack([timeStart, heightStart, widthStart], dim: 0);
        using torch.Tensor ends = torch.stack([timeEnd, heightEnd, widthEnd], dim: 0);
        using torch.Tensor bounds = torch.stack([starts, ends], dim: -1);

        return bounds.unsqueeze(0).contiguous().MoveToOuterDisposeScope();
    }

    private static torch.Tensor ScaleTime(torch.Tensor frameIndex, int temporalFactor, double framesPerSecond)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor pixelFrame = frameIndex.mul(temporalFactor);
        using torch.Tensor shiftedFrame = pixelFrame.add(1.0 - temporalFactor);
        using torch.Tensor causalFrame = shiftedFrame.clamp_min(0.0);

        return causalFrame.div(framesPerSecond).MoveToOuterDisposeScope();
    }
}
