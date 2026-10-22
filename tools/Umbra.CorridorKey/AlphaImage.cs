using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra.CorridorKey;

internal static class AlphaImage
{
    private static readonly double[] NormalizationMean = [0.485, 0.456, 0.406];
    private static readonly double[] NormalizationDeviation = [0.229, 0.224, 0.225];

    public static torch.Tensor Prepare(torch.Tensor colors, torch.Tensor hint, int imageSize)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor scaledColors = Resample(colors, imageSize);
        using torch.Tensor scaledHint = Resample(hint, imageSize);
        using torch.Tensor mean = BuildChannels(NormalizationMean, colors.device);
        using torch.Tensor deviation = BuildChannels(NormalizationDeviation, colors.device);
        using torch.Tensor centeredColors = scaledColors.sub(mean);
        using torch.Tensor normalizedColors = centeredColors.div(deviation);

        return torch.cat([normalizedColors, scaledHint], dim: 1).MoveToOuterDisposeScope();
    }

    private static torch.Tensor Resample(torch.Tensor planes, int imageSize)
    {
        return functional.interpolate(
            planes,
            size: [imageSize, imageSize],
            mode: torch.InterpolationMode.Bilinear,
            align_corners: false,
            antialias: true);
    }

    private static torch.Tensor BuildChannels(double[] values, torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor channels = torch.tensor(values, torch.ScalarType.Float32, device);

        return channels.view(1, values.Length, 1, 1).MoveToOuterDisposeScope();
    }
}
