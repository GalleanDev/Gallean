using Avalonia.Media;
using Avalonia.Styling;

namespace Gallean.Client;

internal sealed class BitmapCacheTemplate : ITemplate
{
    public bool SnapsToDevicePixels { get; set; }

    public object Build()
    {
        return new BitmapCache { SnapsToDevicePixels = SnapsToDevicePixels };
    }
}
