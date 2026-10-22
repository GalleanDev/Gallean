using Avalonia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class BattleRenderer
{
    private readonly record struct StormFlash(Point Position, double Intensity);

    private const double StormPulseGapMinimum = 0.4;
    private const double StormRestrikeMinimum = 0.3;
    private const double StormAfterglowSpan = 4.0;
    private const double StormIntensityLimit = 1.5;

    private static StormFlash ResolveStormFlash(BattleStormConfiguration storm, TimeSpan loopTime)
    {
        int slot = (int)Math.Floor(loopTime / storm.StrikeInterval);

        if (GetStormRoll(slot, 0) < storm.StrikeChance)
        {
            TimeSpan strikeSpan = (storm.PulseGap * storm.MaximumPulses) + (storm.AfterglowDecay * StormAfterglowSpan);
            TimeSpan slack = storm.StrikeInterval - strikeSpan;
            TimeSpan start = (storm.StrikeInterval * slot) + (slack * GetStormRoll(slot, 1));
            double x = storm.Left + ((storm.Right - storm.Left) * GetStormRoll(slot, 2));
            double intensity = GetStormIntensity(storm, slot, loopTime - start);

            return new StormFlash(new Point(x, storm.Height), Math.Min(intensity, StormIntensityLimit));
        }
        else
        {
            return new StormFlash(new Point(0, 0), 0);
        }
    }

    private static double GetStormIntensity(BattleStormConfiguration storm, int slot, TimeSpan elapsed)
    {
        int pulses = 1 + (int)(GetStormRoll(slot, 3) * storm.MaximumPulses);
        double intensity = 0;
        TimeSpan offset = TimeSpan.Zero;

        if (elapsed >= TimeSpan.Zero)
        {
            intensity += storm.AfterglowLevel * Math.Exp(-(elapsed / storm.AfterglowDecay));
        }

        for (int pulse = 0; pulse < pulses; pulse++)
        {
            TimeSpan since = elapsed - offset;

            if (since >= TimeSpan.Zero)
            {
                intensity += GetStormPulseLevel(slot, pulse) * Math.Exp(-(since / storm.PulseDecay));
            }

            offset += storm.PulseGap * (StormPulseGapMinimum + ((1 - StormPulseGapMinimum) * GetStormRoll(slot, 4 + (2 * pulse))));
        }

        return intensity;
    }

    private static double GetStormPulseLevel(int slot, int pulse)
    {
        if (pulse == 0)
        {
            return 1;
        }
        else
        {
            return StormRestrikeMinimum + ((1 - StormRestrikeMinimum) * GetStormRoll(slot, 5 + (2 * pulse)));
        }
    }

    private static double GetStormRoll(int slot, int salt)
    {
        return StableHash.Combine(slot, salt) / (double)int.MaxValue;
    }

    private static SKPoint3 Scale(SKPoint3 value, double factor)
    {
        return new SKPoint3((float)(value.X * factor), (float)(value.Y * factor), (float)(value.Z * factor));
    }
}
