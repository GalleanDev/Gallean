using Avalonia;
using Avalonia.Controls.Primitives;

namespace Gallean.Client;

internal sealed class ModalIcon : TemplatedControl
{
    public static readonly StyledProperty<IconMoniker> IconProperty =
        AvaloniaProperty.Register<ModalIcon, IconMoniker>(nameof(Icon));

    public IconMoniker Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}
