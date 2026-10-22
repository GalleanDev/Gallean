namespace Gallean.Engine;

internal static class DrainOverflowCalculator
{
    public static IReadOnlyList<int> Distribute(int overflow, IReadOnlyList<int> capacities)
    {
        int[] healed = new int[capacities.Count];

        while (overflow > 0)
        {
            int woundedCount = 0;

            for (int i = 0; i < healed.Length; i++)
            {
                if (healed[i] < capacities[i])
                {
                    woundedCount++;
                }
            }

            if (woundedCount == 0)
            {
                break;
            }

            int share = Math.Max(overflow / woundedCount, 1);

            for (int i = 0; i < healed.Length && overflow > 0; i++)
            {
                int amount = Math.Min(share, capacities[i] - healed[i]);

                healed[i] += amount;
                overflow -= amount;
            }
        }

        return healed;
    }
}
