using Gallean.Common;

namespace Gallean.Engine;

internal sealed class FixedRandomProvider : IRandomProvider
{
    public int Next()
    {
        return int.MaxValue / 2;
    }

    public int Next(int maxValue)
    {
        return maxValue / 2;
    }

    public int Next(int minValue, int maxValue)
    {
        return (minValue + maxValue) / 2;
    }

    public double NextDouble()
    {
        return 0.5;
    }

    public void Shuffle<TItem>(TItem[] items)
    {
        Shuffle(items.AsSpan());
    }

    public void Shuffle<TItem>(Span<TItem> items)
    {
        for (int index = items.Length - 1; index > 0; index--)
        {
            int swapIndex = Next(index + 1);
            (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
        }
    }
}
