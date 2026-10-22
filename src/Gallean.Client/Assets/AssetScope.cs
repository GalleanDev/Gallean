using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed class AssetScope : AvaloniaObject
{
    public static readonly AttachedProperty<IAssetProvider?> AssetsProperty =
        AvaloniaProperty.RegisterAttached<AssetScope, Control, IAssetProvider?>("Assets", inherits: true);

    public static void SetAssets(Control element, IAssetProvider? value)
    {
        element.SetValue(AssetsProperty, value);
    }

    public static IAssetProvider GetRequiredAssets(Control element)
    {
        IAssetProvider? assets = GetOptionalAssets(element);

        if (assets is null)
        {
            throw new KeyNotFoundException("Asset scope is not set.");
        }

        return assets;
    }

    public static IAssetProvider? GetOptionalAssets(Control element)
    {
        return element.GetValue(AssetsProperty);
    }
}
