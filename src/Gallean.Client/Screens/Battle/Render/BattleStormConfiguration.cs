using Avalonia.Media;

namespace Gallean.Client;

internal sealed record BattleStormConfiguration(
    Color Color,
    double Power,
    double Exposure,
    double SkyGain,
    TimeSpan StrikeInterval,
    double StrikeChance,
    double Left,
    double Right,
    double Height,
    double Standoff,
    double Range,
    int MaximumPulses,
    TimeSpan PulseGap,
    TimeSpan PulseDecay,
    double AfterglowLevel,
    TimeSpan AfterglowDecay)
{
    public static BattleStormConfiguration MakeDefault()
    {
        return new BattleStormConfiguration(
            Theme.Battle.Light.StormColor,
            Power: 9.0,
            Exposure: 0.6,
            SkyGain: 0.06,
            StrikeInterval: TimeSpan.FromSeconds(4.5),
            StrikeChance: 0.45,
            Left: -100.0,
            Right: 1050.0,
            Height: -380.0,
            Standoff: 450.0,
            Range: 1800.0,
            MaximumPulses: 4,
            PulseGap: TimeSpan.FromSeconds(0.14),
            PulseDecay: TimeSpan.FromSeconds(0.07),
            AfterglowLevel: 0.18,
            AfterglowDecay: TimeSpan.FromSeconds(0.35));
    }
}
