using Gallean.Common;

namespace Gallean.Client.Tests;

internal sealed class NullAssetPreloader : IAssetPreloader, IStaticInstance<NullAssetPreloader>
{
    public async Task<IDisposable> PreloadAsync(IEnumerable<AssetPreloadItem> items)
    {
        return new CleanupList();
    }
}
