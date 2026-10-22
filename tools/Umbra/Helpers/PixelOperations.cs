using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra;

internal static class PixelOperations
{
    private const double MinimumCoverage = 1e-6;
    private const double NeighborhoodSize = 9.0;
    private const double TexelCenterOffset = 0.5;
    private const int LanczosRadius = 3;

    public static torch.Tensor SpreadColor(torch.Tensor planes)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor result = torch.empty_like(planes);

        for (long frame = 0; frame < planes.shape[0]; frame++)
        {
            using IDisposable frameScope = torch.NewDisposeScope();

            using torch.Tensor source = planes.narrow(0, frame, 1);
            using torch.Tensor colors = source.narrow(1, 0, 3);
            using torch.Tensor alpha = source.narrow(1, 3, 1);
            using torch.Tensor coverage = alpha.gt(0.0).to(planes.dtype);
            using torch.Tensor filledColors = PushPull(colors, coverage);
            using torch.Tensor target = result.narrow(0, frame, 1);
            using torch.Tensor targetColors = target.narrow(1, 0, 3);
            using torch.Tensor targetAlpha = target.narrow(1, 3, 1);

            targetColors.copy_(filledColors);
            targetAlpha.copy_(alpha);
        }

        return result.MoveToOuterDisposeScope();
    }

    public static torch.Tensor Antialias(torch.Tensor alpha, int scale)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor support = alpha.gt(0.0).to(alpha.dtype);
        torch.Tensor silhouette = functional.interpolate(
            support,
            scale_factor: [scale, scale],
            mode: torch.InterpolationMode.Nearest);
        torch.Tensor distance = SignedDistance(silhouette, horizon: 2 * scale);
        torch.Tensor coverage = SmoothToScale(distance, scale).add(TexelCenterOffset).clamp(0.0, 1.0);
        torch.Tensor translucency = functional.interpolate(
            PushPull(alpha, support),
            scale_factor: [scale, scale],
            mode: torch.InterpolationMode.Bilinear,
            align_corners: false);

        return coverage.mul(translucency).MoveToOuterDisposeScope();
    }

    public static torch.Tensor Resize(torch.Tensor planes, long targetHeight, long targetWidth)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor rows = BuildLanczosWeights(planes.shape[2], targetHeight, planes.device);
        using torch.Tensor columns = BuildLanczosWeights(planes.shape[3], targetWidth, planes.device);
        using torch.Tensor floatPlanes = planes.to(torch.ScalarType.Float32);
        using torch.Tensor intermediate = torch.matmul(rows, floatPlanes);

        torch.Tensor result = torch.matmul(intermediate, columns.transpose(0, 1));

        return result.to(planes.dtype).MoveToOuterDisposeScope();
    }

    private static torch.Tensor SignedDistance(torch.Tensor silhouette, int horizon)
    {
        torch.Tensor clearDistance = MeasureDistance(silhouette.neg().add(1.0), horizon);
        torch.Tensor coveredDistance = MeasureDistance(silhouette, horizon);

        return torch.where(
            silhouette.gt(0.5),
            clearDistance.sub(TexelCenterOffset),
            coveredDistance.neg().add(TexelCenterOffset));
    }

    private static torch.Tensor MeasureDistance(torch.Tensor target, int horizon)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor verticalDistance = torch.full_like(target, horizon);

        for (int offset = -horizon; offset <= horizon; offset++)
        {
            using IDisposable stepScope = torch.NewDisposeScope();

            using torch.Tensor previous = verticalDistance;
            using torch.Tensor shiftedTarget = Shift(target, 0, offset, fill: 0);
            using torch.Tensor candidate = torch.where(
                shiftedTarget.gt(0.5), torch.full_like(target, Math.Abs(offset)), torch.full_like(target, horizon));

            verticalDistance = torch.minimum(verticalDistance, candidate).MoveToOuterDisposeScope();
        }

        torch.Tensor distance = torch.full_like(target, horizon);

        for (int offset = -horizon; offset <= horizon; offset++)
        {
            using IDisposable stepScope = torch.NewDisposeScope();

            using torch.Tensor previous = distance;
            using torch.Tensor column = Shift(verticalDistance, offset, 0, fill: horizon);

            distance = torch.minimum(distance, column.pow(2).add(offset * offset).sqrt()).MoveToOuterDisposeScope();
        }

        verticalDistance.Dispose();

        return distance.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Shift(torch.Tensor source, int offsetX, int offsetY, double fill)
    {
        torch.Tensor paddedSource = functional.pad(
            source,
            [Math.Max(offsetX, 0), Math.Max(-offsetX, 0), Math.Max(offsetY, 0), Math.Max(-offsetY, 0)],
            PaddingModes.Constant,
            fill);

        return paddedSource
            .narrow(2, Math.Max(-offsetY, 0), source.shape[2])
            .narrow(3, Math.Max(-offsetX, 0), source.shape[3]);
    }

    private static torch.Tensor SmoothToScale(torch.Tensor distance, int scale)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor interior = distance.clamp_min(0.0);
        using torch.Tensor thickness = functional.max_pool2d(
            interior, kernel_size: (2L * scale) + 1, stride: 1, padding: scale);
        using torch.Tensor targetRadius = thickness.sub(TexelCenterOffset).clamp(0.0, scale);

        torch.Tensor result = torch.zeros_like(distance);

        for (int radius = 0; radius <= scale; radius++)
        {
            using IDisposable stepScope = torch.NewDisposeScope();

            using torch.Tensor previous = result;
            using torch.Tensor level = radius == 0 ? distance.alias() : Tent(distance, radius);
            using torch.Tensor weight = targetRadius.sub(radius).abs().neg().add(1.0).clamp(0.0, 1.0);

            result = result.add(level.mul(weight)).MoveToOuterDisposeScope();
        }

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Tent(torch.Tensor planes, int radius)
    {
        torch.Tensor paddedPlanes = functional.pad(planes, [radius, radius, radius, radius], PaddingModes.Replicate);
        torch.Tensor averagedPlanes = functional.avg_pool2d(paddedPlanes, kernel_size: radius + 1, stride: 1);

        return functional.avg_pool2d(averagedPlanes, kernel_size: radius + 1, stride: 1);
    }

    private static torch.Tensor PushPull(torch.Tensor colors, torch.Tensor coverage)
    {
        List<torch.Tensor> coverageLevels = [coverage];
        List<torch.Tensor> colorLevels = [colors.mul(coverage)];

        while (coverageLevels[0].shape[2] > 1 || coverageLevels[0].shape[3] > 1)
        {
            long[] kernel =
            [
                Math.Min(2, coverageLevels[0].shape[2]),
                Math.Min(2, coverageLevels[0].shape[3])
            ];

            coverageLevels.Insert(0, functional.avg_pool2d(coverageLevels[0], kernel, kernel, ceil_mode: true));
            colorLevels.Insert(0, functional.avg_pool2d(colorLevels[0], kernel, kernel, ceil_mode: true));
        }

        torch.Tensor estimate = torch.zeros_like(colorLevels[0]);

        for (int level = 0; level < coverageLevels.Count; level++)
        {
            using IDisposable levelScope = torch.NewDisposeScope();

            using torch.Tensor coarseEstimate = functional.interpolate(
                estimate,
                size: [coverageLevels[level].shape[2], coverageLevels[level].shape[3]],
                mode: torch.InterpolationMode.Nearest);

            using torch.Tensor neighborColor = SumNeighborhood(colorLevels[level]);
            using torch.Tensor neighborCoverage = SumNeighborhood(coverageLevels[level]);
            using torch.Tensor blend = neighborCoverage.clamp(0.0, 1.0);
            using torch.Tensor neighborAverage = neighborColor.div(neighborCoverage.clamp_min(MinimumCoverage));
            using torch.Tensor levelColors = colorLevels[level].div(coverageLevels[level].clamp_min(MinimumCoverage));
            using torch.Tensor inverse = blend.neg().add(1.0);
            using torch.Tensor neighborContribution = neighborAverage.mul(blend);
            using torch.Tensor coarseContribution = coarseEstimate.mul(inverse);
            using torch.Tensor mixture = neighborContribution.add(coarseContribution);
            using torch.Tensor selector = coverageLevels[level].gt(0.0);

            estimate.Dispose();

            estimate = torch.where(selector, levelColors, mixture).MoveToOuterDisposeScope();
        }

        return torch.where(coverage.amax([2, 3], keepdim: true).gt(0.0), estimate, colors);
    }

    private static torch.Tensor SumNeighborhood(torch.Tensor planes)
    {
        return functional.avg_pool2d(planes, kernel_size: 3, stride: 1, padding: 1).mul(NeighborhoodSize);
    }

    private static torch.Tensor BuildLanczosWeights(long source, long target, torch.Device device)
    {
        double step = (double)source / target;
        double stretch = Math.Max(1, step);
        double reach = LanczosRadius * stretch;
        float[] weights = new float[target * source];

        for (long row = 0; row < target; row++)
        {
            double center = ((row + 0.5) * step) - 0.5;
            long first = Math.Max(0, (long)Math.Ceiling(center - reach));
            long last = Math.Min(source - 1, (long)Math.Floor(center + reach));
            double total = 0;

            for (long column = first; column <= last; column++)
            {
                total += LanczosKernel((column - center) / stretch);
            }

            for (long column = first; column <= last; column++)
            {
                weights[(row * source) + column] = (float)(LanczosKernel((column - center) / stretch) / total);
            }
        }

        return torch.tensor(weights, [target, source], torch.ScalarType.Float32, device);
    }

    private static double LanczosKernel(double distance)
    {
        double magnitude = Math.Abs(distance);

        if (magnitude < LanczosRadius)
        {
            return Sinc(magnitude) * Sinc(magnitude / LanczosRadius);
        }
        else
        {
            return 0;
        }
    }

    private static double Sinc(double value)
    {
        if (value == 0)
        {
            return 1;
        }
        else
        {
            return Math.Sin(Math.PI * value) / (Math.PI * value);
        }
    }
}
