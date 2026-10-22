namespace Gallean.Client;

internal interface IAssetPreloader
{
    Task<IDisposable> PreloadAsync(IEnumerable<AssetPreloadItem> items);
}
