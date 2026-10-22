namespace Gallean.Engine;

internal static partial class UtilityScore
{
    public const int MaxScore = 1000;
    public const int MinScore = 1;

    public static int Combine(IEnumerable<Consideration> considerations)
    {
        int finalScore = MaxScore;

        foreach (Consideration consideration in considerations)
        {
            int score = consideration.Score;

            if (score == 0)
            {
                return 0;
            }

            finalScore = (finalScore * score + MaxScore / 2) / MaxScore;
        }

        return Math.Max(finalScore, MinScore);
    }
}
