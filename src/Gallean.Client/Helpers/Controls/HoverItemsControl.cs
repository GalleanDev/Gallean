using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Gallean.Client;

internal sealed partial class HoverItemsControl : ItemsControl
{
    [GeneratedDirectProperty]
    public partial object? HoveredItem { get; private set; }

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
