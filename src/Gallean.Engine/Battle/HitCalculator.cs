using Gallean.Common;

namespace Gallean.Engine;

internal static class HitCalculator
{
    private const int AccuracyMax = 100;

    public static bool RollHit(int accuracy, IRandomProvider random)
    {
        int roll1 = random.Next(AccuracyMax);
        int roll2 = random.Next(AccuracyMax);
        int average = (roll1 + roll2) / 2;

        return average <= accuracy;
    }
}
