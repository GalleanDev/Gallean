using Gallean.Common;

namespace Gallean.Client;

internal sealed class AssetPreloader(IAssetProvider assets) : IAssetPreloader
{
    private const int MaxParallelLoads = 4;

    public async Task<IDisposable> PreloadAsync(IEnumerable<AssetPreloadItem> items)
    {
        ParallelOptions options = new() { MaxDegreeOfParallelism = MaxParallelLoads };
        Lock locker = new();
        CleanupList leases = new();

        try
        {
            await Parallel.ForEachAsync(items.Distinct(), options, async (item, _) =>
            {
                IDisposable? lease = item.Preload(assets);

                if (lease is not null)
                {
                    lock (locker)
                    {
                        leases.Add(lease);
                    }
                }
            });
        }
        catch
        {
            leases.Dispose();

            throw;
        }

        return leases;
    }
}
