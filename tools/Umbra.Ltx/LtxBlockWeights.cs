using TorchSharp;

namespace Umbra.Ltx;

internal sealed class LtxBlockWeights(
    LtxBlockSource source,
    LtxLora? lora,
    string blocksPrefix,
    int cachedBlocks,
    torch.ScalarType type,
    torch.Device device)
    : IDisposable
{
    private readonly LtxBlockPool pool = new(source.Layout.Elements, Math.Min(cachedBlocks, source.Count), type, device);
    private readonly Dictionary<int, LtxCachedBlock> cached = [];
    private readonly Queue<int> loaded = [];

    public IReadOnlyDictionary<string, torch.Tensor> Get(int index)
    {
        if (cached.TryGetValue(index, out LtxCachedBlock? block))
        {
            return block.Views;
        }

        if (cached.Count == pool.Capacity)
        {
            int evicted = loaded.Dequeue();
            LtxCachedBlock stale = cached[evicted];

            cached.Remove(evicted);
            stale.Dispose();
            pool.Release(stale.Buffer);
        }

        block = Load(index);
        cached[index] = block;
        loaded.Enqueue(index);

        return block.Views;
    }

    public void Dispose()
    {
        foreach (LtxCachedBlock block in cached.Values)
        {
            block.Dispose();
        }

        cached.Clear();
        loaded.Clear();
        pool.Dispose();
        lora?.Dispose();
        source.Dispose();
    }

    private LtxCachedBlock Load(int index)
    {
        using (torch.no_grad())
        {
            torch.Tensor buffer = pool.Acquire();
            Dictionary<string, torch.Tensor> views = source.Layout.Carve(buffer);

            foreach ((string name, torch.Tensor weight) in source.GetBlock(index))
            {
                views[name].copy_(weight, nonBlocking: true);
            }

            lora?.FuseInto($"{blocksPrefix}{index}.", views);

            foreach (torch.Tensor view in views.Values)
            {
                view.DetachFromDisposeScope();
            }

            return new LtxCachedBlock(buffer, views);
        }
    }
}
