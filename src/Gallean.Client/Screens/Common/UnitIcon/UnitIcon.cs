using Avalonia;
using Avalonia.Controls.Primitives;

namespace Gallean.Client;

internal sealed class UnitIcon : TemplatedControl
{
    public static readonly StyledProperty<AssetSpriteKey?> SpriteKeyProperty =
        AvaloniaProperty.Register<UnitIcon, AssetSpriteKey?>(nameof(SpriteKey));

    public AssetSpriteKey? SpriteKey
    {
        get => GetValue(SpriteKeyProperty);
        set => SetValue(SpriteKeyProperty, value);
    }
}
