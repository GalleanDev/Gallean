namespace Umbra;

internal sealed record TeaCacheOptions(
    double Threshold,
    IReadOnlyList<double> Coefficients,
    int RetainForwards,
    int CutoffForwards)
{
    public const double DefaultThreshold = 0.25;

    public static TeaCacheOptions None { get; } = new(0, [], 0, 0);

    public static TeaCacheOptions WanTextToVideoSmall(double threshold, int denoisingSteps)
    {
        int forwards = denoisingSteps * 2;

        return new TeaCacheOptions(
            threshold,
            [2.39676752e+03, -1.31110545e+03, 2.01331979e+02, -8.29855975e+00, 1.37887774e-01],
            2,
            forwards - 2);
    }
}
