using Avalonia;
using Avalonia.Media;
using Gallean.Common;

namespace Gallean.Client;

internal sealed record BattleLightConfiguration(
    Vector3D SunDirection,
    Color SunColor,
    double SunPower,
    TimeSpan Cycle,
    IReadOnlyList<BattleLight> RigLights,
    double SpriteLightStandoff,
    double SpriteLightReach,
    double SpriteLightMinimumRange,
    double SpriteLightGain,
    double ShadowContrast,
    double ShadowRiseLimit,
    double ShadowPenumbraLimit,
    double UnitSkyGain,
    double BackgroundAmbient,
    double BackgroundShadingCap,
    double BackgroundRollOffThreshold,
    double BackgroundSpriteLightBoost,
    double BackgroundSpriteLightReach,
    BattleStormConfiguration? Storm)
{
    public bool Equals(BattleLightConfiguration? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return SunDirection == other.SunDirection &&
               SunColor == other.SunColor &&
               SunPower.Equals(other.SunPower) &&
               Cycle == other.Cycle &&
               RigLights.SequenceEqual(other.RigLights) &&
               SpriteLightStandoff.Equals(other.SpriteLightStandoff) &&
               SpriteLightReach.Equals(other.SpriteLightReach) &&
               SpriteLightMinimumRange.Equals(other.SpriteLightMinimumRange) &&
               SpriteLightGain.Equals(other.SpriteLightGain) &&
               ShadowContrast.Equals(other.ShadowContrast) &&
               ShadowRiseLimit.Equals(other.ShadowRiseLimit) &&
               ShadowPenumbraLimit.Equals(other.ShadowPenumbraLimit) &&
               UnitSkyGain.Equals(other.UnitSkyGain) &&
               BackgroundAmbient.Equals(other.BackgroundAmbient) &&
               BackgroundShadingCap.Equals(other.BackgroundShadingCap) &&
               BackgroundRollOffThreshold.Equals(other.BackgroundRollOffThreshold) &&
               BackgroundSpriteLightBoost.Equals(other.BackgroundSpriteLightBoost) &&
               BackgroundSpriteLightReach.Equals(other.BackgroundSpriteLightReach) &&
               Storm == other.Storm;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(SunDirection);
        hash.Add(SunColor);
        hash.Add(SunPower);
        hash.Add(Cycle);
        hash.AddSequence(RigLights);
        hash.Add(SpriteLightStandoff);
        hash.Add(SpriteLightReach);
        hash.Add(SpriteLightMinimumRange);
        hash.Add(SpriteLightGain);
        hash.Add(ShadowContrast);
        hash.Add(ShadowRiseLimit);
        hash.Add(ShadowPenumbraLimit);
        hash.Add(UnitSkyGain);
        hash.Add(BackgroundAmbient);
        hash.Add(BackgroundShadingCap);
        hash.Add(BackgroundRollOffThreshold);
        hash.Add(BackgroundSpriteLightBoost);
        hash.Add(BackgroundSpriteLightReach);
        hash.Add(Storm);

        return hash.ToHashCode();
    }

    public static BattleLightConfiguration MakeDawn(IReadOnlyList<BattleLight> lights)
    {
        return Make(new Vector3D(-0.80, -0.35, 0.49), Theme.Battle.Light.SunDawnColor, sunPower: 1.8, lights);
    }

    public static BattleLightConfiguration MakeMidday(IReadOnlyList<BattleLight> lights)
    {
        return Make(new Vector3D(0.05, -0.90, 0.43), Theme.Battle.Light.SunMiddayColor, sunPower: 2.2, lights);
    }

    public static BattleLightConfiguration MakeEvening(IReadOnlyList<BattleLight> lights)
    {
        return Make(new Vector3D(0.80, -0.35, 0.49), Theme.Battle.Light.SunEveningColor, sunPower: 1.9, lights);
    }

    public static BattleLightConfiguration MakeDusk(IReadOnlyList<BattleLight> lights)
    {
        return Make(new Vector3D(0.0, -0.85, 0.53), Theme.Battle.Light.SkyDuskColor, sunPower: 0.7, lights) with
        {
            ShadowContrast = 7
        };
    }

    private static BattleLightConfiguration Make(
        Vector3D sunDirection,
        Color sunColor,
        double sunPower,
        IReadOnlyList<BattleLight> lights)
    {
        return new BattleLightConfiguration(
            Vector3D.Normalize(sunDirection),
            sunColor,
            sunPower,
            Cycle: TimeSpan.FromSeconds(8.0),
            RigLights: [.. MakeFillLights(), .. lights],
            SpriteLightStandoff: 10.0,
            SpriteLightReach: 4.0,
            SpriteLightMinimumRange: 40.0,
            SpriteLightGain: 2.25,
            ShadowContrast: 2.2,
            ShadowRiseLimit: 0.5,
            ShadowPenumbraLimit: 12.0,
            UnitSkyGain: 0.2,
            BackgroundAmbient: 0.22,
            BackgroundShadingCap: 0.5,
            BackgroundRollOffThreshold: 0.85,
            BackgroundSpriteLightBoost: 2.25,
            BackgroundSpriteLightReach: 0.75,
            Storm: null);
    }

    private static IReadOnlyList<BattleLight> MakeFillLights()
    {
        return
        [
            new BattleLight(
                new Point(900, -260),
                Standoff: 820,
                Range: 1400,
                Theme.Battle.Light.MoonColor,
                Power: 2.6,
                Sway: new Vector(0, 0),
                SwayRate: new Vector(0, 0),
                SwayPhase: new Vector(0, 0),
                Flicker: 0.0,
                FlickerRate: 0),
            new BattleLight(
                new Point(100, 100),
                Standoff: 600,
                Range: 1200,
                Theme.Battle.Light.SkyColor,
                Power: 0.7,
                Sway: new Vector(0, 0),
                SwayRate: new Vector(0, 0),
                SwayPhase: new Vector(0, 0),
                Flicker: 0.0,
                FlickerRate: 0)
        ];
    }
}
