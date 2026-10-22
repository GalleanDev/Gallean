namespace Gallean.Assets;

public sealed partial class AssetCache<TContent>
{
    public sealed partial class Lease : IDisposable
    {
        private readonly AssetCache<TContent> cache;
        private Entry? entry;

        private Lease(AssetCache<TContent> cache)
        {
            this.cache = cache;
        }

        public TContent Value { get; private set; } = null!;

        public void Dispose()
        {
            Entry? released = Interlocked.Exchange(ref entry, null);

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
