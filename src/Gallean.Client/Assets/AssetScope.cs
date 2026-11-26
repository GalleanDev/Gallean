using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class AssetScope : AvaloniaObject
{
    [GeneratedAttachedProperty(Inherits = true)]
    public static partial IAssetProvider? GetAssets(Control element);

    public static IAssetProvider GetRequiredAssets(Control element)
    {
        IAssetProvider? assets = GetAssets(element);

        if (assets is null)
        {
            throw new KeyNotFoundException("Asset scope is not set.");
        }

        return assets;
    }
}
