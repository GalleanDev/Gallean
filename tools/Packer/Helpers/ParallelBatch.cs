using System.Runtime.ExceptionServices;

namespace Packer;

internal static class ParallelBatch
{
    public static IEnumerable<(TItem Item, TPrepared Prepared)> Prepare<TItem, TPrepared>(
        IReadOnlyList<TItem> items,
        ParallelOptions options,
        Func<TItem, long> getCost,
        long maxBatchCost,
        Func<TItem, TPrepared> prepare)
    {
        int batchStart = 0;

        while (batchStart < items.Count)
        {
            int batchCount = GetBatchCount(items, getCost, maxBatchCost, batchStart);
            TPrepared[] prepared = new TPrepared[batchCount];

            try
            {
                Parallel.For(0, batchCount, options, batchIndex =>
                {
                    prepared[batchIndex] = prepare(items[batchStart + batchIndex]);
                });
            }
            catch (AggregateException ex)
            {
                AggregateException flattened = ex.Flatten();

                ExceptionDispatchInfo.Throw(flattened.InnerExceptions[0]);
            }

            for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                yield return (items[batchStart + batchIndex], prepared[batchIndex]);
            }

            batchStart += batchCount;
        }
    }

    private static int GetBatchCount<TItem>(
        IReadOnlyList<TItem> items,
        Func<TItem, long> getCost,
        long maxBatchCost,
        int batchStart)
    {
        long cost = 0;
        int count = 0;

        while (batchStart + count < items.Count)
        {
            long itemCost = getCost(items[batchStart + count]);

            if (count > 0 && cost + itemCost > maxBatchCost)
            {
                break;
            }

            cost += itemCost;
            count++;
        }

        return count;
    }
}
