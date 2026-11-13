namespace Gallean.Assets;

public sealed partial class AssetCache<TContent>
{
    public sealed partial class Lease : IDisposable
    {
        private readonly AssetCache<TContent> cache;
        private LinkedListNode<Entry>? node;

        private Lease(AssetCache<TContent> cache)
        {
            this.cache = cache;
        }

        public TContent Value { get; private set; } = null!;

        public void Dispose()
        {
            LinkedListNode<Entry>? released = Interlocked.Exchange(ref node, null);

            if (released is not null)
            {
                cache.Release(released, this);
            }
        }

        public static implicit operator TContent(Lease lease)
        {
            return lease.Value;
        }
    }
}
