using Gallean.Common;

namespace Gallean.Engine.Tests;

internal sealed class TestSequenceRandomProvider(params int[] values) : IRandomProvider
{
    private readonly Queue<int> values = new(values);

    public int Next()
    {
        return values.Dequeue();
    }

    public int Next(int maxValue)
    {
        return values.Dequeue();
    }

    public int Next(int minValue, int maxValue)
    {
        return values.Dequeue();
    }

    public double NextDouble()
    {
        throw new NotSupportedException();
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
