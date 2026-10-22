using Avalonia.Media;

namespace Gallean.Client;

internal sealed record BattleFluidConfiguration(
    AnimationRate Rate,
    Color DeepColor,
    Color BodyColor,
    Color PeakColor,
    Color ScatterColor,
    double Gain,
    double Extinction,
    double Powder,
    double PowderFloor,
    double BodyRamp,
    double PeakRamp,
    double PeakOffset)
{
    public static BattleFluidConfiguration MakeLava()
    {
        return new BattleFluidConfiguration(
            new AnimationRate(12),
            Theme.Battle.Fluid.LavaDeepColor,
            Theme.Battle.Fluid.LavaBodyColor,
            Theme.Battle.Fluid.LavaPeakColor,
            Theme.Battle.Fluid.LavaScatterColor,
            Gain: 3.0,
            Extinction: 2.5,
            Powder: 3.0,
            PowderFloor: 1.0,
            BodyRamp: 1.1,
            PeakRamp: 0.7,
            PeakOffset: 0.8);
    }

    public static BattleFluidConfiguration MakeVapor()
    {
        return new BattleFluidConfiguration(
            new AnimationRate(12),
            Theme.Battle.Fluid.VaporDeepColor,
            Theme.Battle.Fluid.VaporBodyColor,
            Theme.Battle.Fluid.VaporPeakColor,
            Theme.Battle.Fluid.VaporScatterColor,
            Gain: 1.6,
            Extinction: 1.7,
            Powder: 3.0,
            PowderFloor: 0.35,
            BodyRamp: 1.1,
            PeakRamp: 0.7,
            PeakOffset: 0.8);
    }

    public static BattleFluidConfiguration MakeWater()
    {
        return new BattleFluidConfiguration(
            new AnimationRate(12),
            Theme.Battle.Fluid.WaterDeepColor,
            Theme.Battle.Fluid.WaterBodyColor,
            Theme.Battle.Fluid.WaterPeakColor,
            Theme.Battle.Fluid.WaterScatterColor,
            Gain: 3.0,
            Extinction: 2.5,
            Powder: 3.0,
            PowderFloor: 1.0,
            BodyRamp: 1.1,
            PeakRamp: 0.7,
            PeakOffset: 0.8);
    }

    public static BattleFluidConfiguration MakeFog()
    {
        return new BattleFluidConfiguration(
            new AnimationRate(12),
            Theme.Battle.Fluid.FogDeepColor,
            Theme.Battle.Fluid.FogBodyColor,
            Theme.Battle.Fluid.FogPeakColor,
            Theme.Battle.Fluid.FogScatterColor,
            Gain: 3.0,
            Extinction: 1.7,
            Powder: 3.0,
            PowderFloor: 0.35,
            BodyRamp: 1.1,
            PeakRamp: 0.7,
            PeakOffset: 0.8);
    }

    public static BattleFluidConfiguration MakeDust()
    {
        return new BattleFluidConfiguration(
            new AnimationRate(12),
            Theme.Battle.Fluid.DustDeepColor,
            Theme.Battle.Fluid.DustBodyColor,
            Theme.Battle.Fluid.DustPeakColor,
            Theme.Battle.Fluid.DustScatterColor,
            Gain: 2.2,
            Extinction: 1.7,
            Powder: 3.0,
            PowderFloor: 0.35,
            BodyRamp: 1.1,
            PeakRamp: 0.7,
            PeakOffset: 0.8);
    }

    public static BattleFluidConfiguration MakeMiasma()
    {
        return new BattleFluidConfiguration(
            new AnimationRate(12),
            Theme.Battle.Fluid.MiasmaDeepColor,
            Theme.Battle.Fluid.MiasmaBodyColor,
            Theme.Battle.Fluid.MiasmaPeakColor,
            Theme.Battle.Fluid.MiasmaScatterColor,
            Gain: 3.0,
            Extinction: 1.7,
            Powder: 3.0,
            PowderFloor: 0.35,
            BodyRamp: 1.1,
            PeakRamp: 0.7,
            PeakOffset: 0.8);
    }
}
