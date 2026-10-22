namespace Umbra;

internal static partial class BasicVsrDeformConv
{
    private readonly record struct Layout(
        long Batch,
        long Channels,
        long Groups,
        long GroupChannels,
        long Taps,
        long KernelHeight,
        long KernelWidth,
        long Height,
        long Width,
        int Padding,
        int Dilation);
}
