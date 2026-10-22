using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Gallean.Client;

internal sealed class HoverItemsControl : ItemsControl
{
    public static readonly StyledProperty<object?> HoveredItemProperty =
        AvaloniaProperty.Register<HoverItemsControl, object?>(nameof(HoveredItem));

    public object? HoveredItem
    {
        get => GetValue(HoveredItemProperty);
        private set => SetValue(HoveredItemProperty, value);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        HoveredItem = FindItem(e.Source as Visual);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        HoveredItem = null;
    }

    private object? FindItem(Visual? source)
    {
        foreach (Visual visual in source?.GetSelfAndVisualAncestors() ?? [])
        {
            if (visual is Control control)
            {
                int index = IndexFromContainer(control);

                if (index >= 0)
                {
                    return Items[index];
                }
            }
        }

        return null;
    }
}
