namespace Umbra;

public sealed record UmbraFluidMedium(
    int PressureIterations,
    double PressureDecay,
    double VelocityDissipation,
    double DensityDissipation,
    double DensitySpread,
    double DensityLimit,
    double Vorticity,
    double Viscosity,
    double SpeedLimit,
    double SourceRate,
    double GravityX,
    double GravityY,
    double DriftX,
    double DriftY,
    double Gust,
    double GustScale,
    double GustRate,
    double RestoreForce,
    double Current,
    int PhaseLength,
    double Grain,
    double Tail,
    double DetailFloor,
    double DetailRange)
{
    public static UmbraFluidMedium MakeGas()
    {
        return new UmbraFluidMedium(
            PressureIterations: 20,
            PressureDecay: 0.8,
            VelocityDissipation: 0.006,
            DensityDissipation: 0.010,
            DensitySpread: 0.05,
            DensityLimit: 2.0,
            Vorticity: 0.08,
            Viscosity: 0.16,
            SpeedLimit: 1.85,
            SourceRate: 0.45,
            GravityX: 0.0,
            GravityY: -0.063,
            DriftX: 0.058,
            DriftY: 0.0,
            Gust: 0.053,
            GustScale: 4.32,
            GustRate: 0.012,
            RestoreForce: 0.140,
            Current: 0.0,
            PhaseLength: 72,
            Grain: 30.6,
            Tail: 1.9,
            DetailFloor: 0.30,
            DetailRange: 1.5);
    }

    public static UmbraFluidMedium MakeLiquid()
    {
        return new UmbraFluidMedium(
            PressureIterations: 0,
            PressureDecay: 0.8,
            VelocityDissipation: 0.1,
            DensityDissipation: 0.002,
            DensitySpread: 0.0,
            DensityLimit: 2.0,
            Vorticity: 0.0,
            Viscosity: 0.30,
            SpeedLimit: 2.38,
            SourceRate: 0.45,
            GravityX: 0.0,
            GravityY: 0.0,
            DriftX: 0.0,
            DriftY: 0.0,
            Gust: 0.0,
            GustScale: 3.24,
            GustRate: 0.006,
            RestoreForce: 0.0,
            Current: 0.17,
            PhaseLength: 72,
            Grain: 40.0,
            Tail: 1.0,
            DetailFloor: -0.4,
            DetailRange: 3.0);
    }
}
