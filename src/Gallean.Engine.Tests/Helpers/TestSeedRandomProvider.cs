using Gallean.Common;

namespace Gallean.Engine.Tests;

internal sealed class TestSeedRandomProvider(int seed = 42) : IRandomProvider
{
    private readonly Random random = new(seed);

    public int Next()
    {
        return random.Next();
    }

    public int Next(int maxValue)
    {
        return random.Next(maxValue);
    }

    public int Next(int minValue, int maxValue)
    {
        return random.Next(minValue, maxValue);
    }

    public double NextDouble()
    {
        return random.NextDouble();
    }

    public void Shuffle<TItem>(TItem[] items)
    {
        random.Shuffle(items);
    }

    public void Shuffle<TItem>(Span<TItem> items)
    {
        random.Shuffle(items);
    }
}
