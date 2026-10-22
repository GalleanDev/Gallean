using Gallean.Common;

namespace Gallean.Client;

internal sealed class FreeThreadedRandomProvider : IRandomProvider
{
    public int Next()
    {
        return Random.Shared.Next();
    }

    public int Next(int maxValue)
    {
        return Random.Shared.Next(maxValue);
    }

    public int Next(int minValue, int maxValue)
    {
        return Random.Shared.Next(minValue, maxValue);
    }

    public double NextDouble()
    {
        return Random.Shared.NextDouble();
    }

    public void Shuffle<TItem>(TItem[] items)
    {
        Random.Shared.Shuffle(items);
    }

    public void Shuffle<TItem>(Span<TItem> items)
    {
        Random.Shared.Shuffle(items);
    }
}
