namespace Gallean.Assets;

public sealed partial class AssetCache<TContent>
{
    public sealed partial class Lease
    {
        internal sealed class Pool
        {
            private readonly Stack<Lease> items = new();

            public Lease Rent(AssetCache<TContent> cache, Entry entry, TContent value)
            {
                Lease lease;

                if (items.Count > 0)
                {
                    lease = items.Pop();
                }
                else
                {
                    lease = new Lease(cache);
                }

                lease.entry = entry;
                lease.Value = value;

                return lease;
            }

            public void Return(Lease lease)
            {
                lease.Value = null!;
                items.Push(lease);
            }
        }
    }
}
