using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed record BattleRainConfiguration(
    AnimationRate Rate,
    BattleRainLayer Backdrop,
    BattleRainLayer Overlay,
    Color StreakColor,
    Color SplashColor,
    Color HazeColor,
    double SkyGain,
    Vector StreakSpacing,
    double StreakFall,
    double StreakSlant,
    double StreakLength,
    double StreakWidth,
    double StreakChance,
    double StreakOpacity,
    double DepthSpread,
    double DepthFade,
    double SplashSpacing,
    double SplashRate,
    double SplashRadius,
    double SplashWidth,
    double SplashChance,
    double GroundFacing,
    double Haze)
{
    public static BattleRainConfiguration MakeDefault()
    {
        return new BattleRainConfiguration(
            new AnimationRate(30.0),
            new BattleRainLayer(NearDepth: 0.5, FarDepth: 1.0, SplashOpacity: 0.45),
            new BattleRainLayer(NearDepth: 0.0, FarDepth: 0.5, SplashOpacity: 0.0),
            Theme.Battle.Rain.StreakColor,
            Theme.Battle.Rain.SplashColor,
            Theme.Battle.Rain.HazeColor,
            SkyGain: 0.086,
            StreakSpacing: new Vector(22.0, 140.0),
            StreakFall: 900.0,
            StreakSlant: 0.22,
            StreakLength: 62.0,
            StreakWidth: 1.1,
            StreakChance: 0.38,
            StreakOpacity: 0.2,
            DepthSpread: 2.2,
            DepthFade: 0.35,
            SplashSpacing: 0.13,
            SplashRate: 1.9,
            SplashRadius: 0.32,
            SplashWidth: 0.08,
            SplashChance: 0.3,
            GroundFacing: 0.6,
            Haze: 0.14);
    }
}
