using Avalonia;
using Avalonia.Controls.Primitives;

namespace Gallean.Client;

internal sealed partial class UnitIcon : TemplatedControl
{
    [GeneratedStyledProperty]
    public partial AssetSpriteKey? SpriteKey { get; set; }
}
