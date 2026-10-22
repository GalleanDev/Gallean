using System.Diagnostics;

namespace Gallean.Assets;

public sealed partial class AssetCache<TContent>(
    long sizeLimit,
    AssetCache<TContent>.LoadFunc loadFunc,
    AssetCache<TContent>.MeasureFunc measureFunc,
    AssetCache<TContent>.DisposeFunc disposeFunc)
    : IDisposable
    where TContent : class
{
    internal sealed class Entry
    {
        public required AssetKey Key { get; init; }
        public required TContent? Content { get; init; }
        public required long Size { get; init; }
        public int ReferenceCount { get; set; }

#if DEBUG
        ~Entry()
        {
            Debug.Assert(ReferenceCount == 0, $"Asset '{Key.CanonicalId}' has {ReferenceCount} outstanding leases.");
        }
#endif
    }

    public delegate TContent? LoadFunc(AssetKey key);

    public delegate long MeasureFunc(TContent content);

    public delegate void DisposeFunc(TContent content);

    private readonly Dictionary<AssetKey, LinkedListNode<Entry>> entries = [];
    private readonly Lease.Pool leasePool = new();
    private readonly Lock locker = new();
    private readonly LinkedList<Entry> recent = [];
    private long evictableSize;
    private bool disposed;

    public int Count
    {
        get
        {
            lock (locker)
            {
                return entries.Count;
            }
        }
    }

    public void Dispose()
    {
        lock (locker)
        {
            disposed = true;

            foreach (Entry entry in recent)
            {
                if (entry.ReferenceCount == 0)
                {
                    DisposeContent(entry);
                }
            }

            entries.Clear();
            recent.Clear();
            evictableSize = 0;
        }
    }

    public Lease? Acquire(AssetKey key)
    {
        lock (locker)
        {
            if (disposed)
            {
                return null;
            }

            if (entries.TryGetValue(key, out LinkedListNode<Entry>? hit))
            {
                MoveToFront(hit);

                return Pin(hit.Value);
            }
        }

        TContent? content = loadFunc(key);

        lock (locker)
        {
            if (disposed)
            {
                if (content is not null)
                {
                    disposeFunc(content);
                }

                return null;
            }

            if (entries.TryGetValue(key, out LinkedListNode<Entry>? raced))
            {
                if (content is not null)
                {
                    disposeFunc(content);
                }

                MoveToFront(raced);

                return Pin(raced.Value);
            }

            long entrySize = 0;

            if (content is not null)
            {
                entrySize = measureFunc(content);
            }

            Entry entry = new() { Key = key, Content = content, Size = entrySize };
            entries[key] = recent.AddFirst(entry);
            evictableSize += entrySize;

            Lease? pinned = Pin(entry);
            Evict();

            return pinned;
        }
    }

    private void Release(Entry entry, Lease lease)
    {
        lock (locker)
        {
            entry.ReferenceCount--;

            Debug.Assert(entry.ReferenceCount >= 0);

            if (disposed)
            {
                if (entry.ReferenceCount == 0)
                {
                    DisposeContent(entry);
                }
            }
            else
            {
                if (entry.ReferenceCount == 0)
                {
                    evictableSize += entry.Size;
                }

                if (evictableSize > sizeLimit)
                {
                    Evict();
                }
            }

            leasePool.Return(lease);
        }
    }

    private Lease? Pin(Entry entry)
    {
        if (entry.Content is null)
        {
            return null;
        }

        if (entry.ReferenceCount == 0)
        {
            evictableSize -= entry.Size;
        }

        entry.ReferenceCount++;

        return leasePool.Rent(this, entry, entry.Content);
    }

    private void Evict()
    {
        while (evictableSize > sizeLimit)
        {
            LinkedListNode<Entry>? node = recent.Last;

            while (node is not null && node.Value.ReferenceCount > 0)
            {
                node = node.Previous;
            }

            if (node is null)
            {
                return;
            }

            recent.Remove(node);
            entries.Remove(node.Value.Key);
            evictableSize -= node.Value.Size;
            DisposeContent(node.Value);
        }
    }

    private void MoveToFront(LinkedListNode<Entry> node)
    {
        if (recent.First != node)
        {
            recent.Remove(node);
            recent.AddFirst(node);
        }
    }

    private void DisposeContent(Entry entry)
    {
        if (entry.Content is not null)
        {
            disposeFunc(entry.Content);
        }
    }
}
