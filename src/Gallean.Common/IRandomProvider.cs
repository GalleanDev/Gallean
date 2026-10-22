namespace Gallean.Common;

public interface IRandomProvider
{
    int Next();
    int Next(int maxValue);
    int Next(int minValue, int maxValue);
    double NextDouble();
    void Shuffle<TItem>(TItem[] items);
    void Shuffle<TItem>(Span<TItem> items);
}
