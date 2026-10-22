namespace Gallean.Engine;

internal static partial class UtilityScore
{
    public sealed record Consideration(int Input, ResponseCurve Curve)
    {
        public int Score => Curve.Evaluate(Input);
    }

    public static Consideration Full { get; } = Of(MaxScore);
    public static Consideration None { get; } = Of(0);

    public static Consideration Of(int value)
    {
        return Of(value, ResponseCurve.Linear);
    }

    public static Consideration Of(int value, ResponseCurve curve)
    {
        return new Consideration(value, curve);
    }

    public static Consideration Flag(bool value, int falseScore = 0)
    {
        int input = value ? 1 : 0;

        return new Consideration(input, new ResponseCurve.Threshold(1, falseScore, MaxScore));
    }

    public static Consideration Ratio(int part, int whole)
    {
        return Ratio(part, whole, ResponseCurve.Linear);
    }

    public static Consideration Ratio(int part, int whole, ResponseCurve curve)
    {
        if (whole <= 0)
        {
            return Full;
        }

        return Of(part * MaxScore / whole, curve);
    }

    public static Consideration InverseRatio(int part, int whole)
    {
        if (whole <= 0)
        {
            return Full;
        }

        return Of(MaxScore - part * MaxScore / whole);
    }
}
