using TorchSharp;

namespace Umbra;

internal sealed class WanVaeCache(int convolutions) : IDisposable
{
    public const int Depth = 2;

    private readonly List<torch.Tensor?> entries = [.. Enumerable.Repeat<torch.Tensor?>(null, convolutions)];
    private readonly HashSet<int> repeated = [];

    public int Position { get; private set; }

    public int Take()
    {
        return Position++;
    }

    public void Rewind()
    {
        Position = 0;
    }

    public bool IsEmpty(int index)
    {
        return entries[index] is null && !repeated.Contains(index);
    }

    public torch.Tensor? Get(int index)
    {
        return entries[index];
    }

    public void Set(int index, torch.Tensor value)
    {
        entries[index]?.Dispose();
        entries[index] = value.DetachFromDisposeScope();
        repeated.Remove(index);
    }

    public void MarkRepeated(int index)
    {
        entries[index]?.Dispose();
        entries[index] = null;
        repeated.Add(index);
    }

    public void Dispose()
    {
        for (int index = 0; index < entries.Count; index++)
        {
            entries[index]?.Dispose();
            entries[index] = null;
        }

        repeated.Clear();
        Position = 0;
    }
}
