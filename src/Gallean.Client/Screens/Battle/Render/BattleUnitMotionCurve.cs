namespace Gallean.Client;

internal static class BattleUnitMotionCurve
{
    private const double MaxValue = 0.2;

    public static double Evaluate(double progress)
    {
        double clamped = Math.Clamp(progress, 0, 1);

        if (clamped < MaxValue)
        {
            return EaseOut(clamped / MaxValue);
        }
        else
        {
            double fall = (clamped - MaxValue) / (1 - MaxValue);

            return 1 - SmoothStep(fall);
        }
    }

    private static double EaseOut(double progress)
    {
        double remaining = 1 - progress;

        return 1 - (remaining * remaining);
    }

    private static double SmoothStep(double progress)
    {
        return progress * progress * (3 - (2 * progress));
    }
}
