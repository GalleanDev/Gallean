namespace Gallean.Common;

public static class CollectionJoin
{
    public readonly record struct Item<TItem>(TItem? Left, TItem? Right) where TItem : class;

    public static IEnumerable<Item<TItem>> Make<TItem, TKey>(
        IReadOnlyList<TItem> left,
        IReadOnlyList<TItem> right,
        Func<TItem, TKey> keyFunc)
        where TItem : class
        where TKey : IComparable<TKey>
    {
        left = [.. left.OrderBy(keyFunc)];
        right = [.. right.OrderBy(keyFunc)];

        int leftIndex = 0;
        int rightIndex = 0;

        while (leftIndex < left.Count && rightIndex < right.Count)
        {
            TKey leftKey = keyFunc(left[leftIndex]);
            TKey rightKey = keyFunc(right[rightIndex]);

            int rv = leftKey.CompareTo(rightKey);

            if (rv > 0)
            {
                yield return new Item<TItem>(null, right[rightIndex]);
                rightIndex++;
            }
            else if (rv < 0)
            {
                yield return new Item<TItem>(left[leftIndex], null);
                leftIndex++;
            }
            else
            {
                yield return new Item<TItem>(left[leftIndex], right[rightIndex]);
                leftIndex++;
                rightIndex++;
            }
        }

        while (leftIndex < left.Count)
        {
            yield return new Item<TItem>(left[leftIndex], null);
            leftIndex++;
        }

        while (rightIndex < right.Count)
        {
            yield return new Item<TItem>(null, right[rightIndex]);
            rightIndex++;
        }
    }
}
