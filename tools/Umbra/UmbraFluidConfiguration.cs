namespace Umbra;

public sealed record UmbraFluidConfiguration(
    UmbraFluidMedium Medium,
    int Seed,
    double SimulationScale,
    double RestoreReach,
    double SourceThreshold,
    double SourceContrast,
    double SourceFrequency,
    int WarmUpSteps,
    int FrameCount);
