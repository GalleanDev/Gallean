namespace GameContent;

internal sealed class BoundedCache<TKey, TValue>(int capacity)
    where TKey : notnull
{
    private sealed record Entry(TKey Key, TValue Value);

    private readonly Dictionary<TKey, LinkedListNode<Entry>> entries = [];
    private readonly Lock locker = new();
    private readonly LinkedList<Entry> recent = [];

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> build)
    {
        lock (locker)
        {
            if (entries.TryGetValue(key, out LinkedListNode<Entry>? hit))
            {
                MoveToFront(hit);

                return hit.Value.Value;
            }
        }

        TValue value = build(key);

        lock (locker)
        {
            if (entries.TryGetValue(key, out LinkedListNode<Entry>? raced))
            {
                MoveToFront(raced);

                return raced.Value.Value;
            }

            entries[key] = recent.AddFirst(new Entry(key, value));
            Evict();

            return value;
        }
    }

    private void Evict()
    {
        while (entries.Count > capacity)
        {
            LinkedListNode<Entry>? node = recent.Last;

            if (node is null)
            {
                return;
            }

            recent.RemoveLast();
            entries.Remove(node.Value.Key);
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
}
