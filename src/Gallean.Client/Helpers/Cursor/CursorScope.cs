using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed class CursorScope : AvaloniaObject
{
    public static readonly AttachedProperty<AssetAnimationKey.CursorType?> CursorTypeProperty =
        AvaloniaProperty.RegisterAttached<CursorScope, Control, AssetAnimationKey.CursorType?>("CursorType", null, inherits: true);

    public static readonly AttachedProperty<CursorDragImage?> DragImageProperty =
        AvaloniaProperty.RegisterAttached<CursorScope, TopLevel, CursorDragImage?>("DragImage");

    public static void SetCursorType(Control element, AssetAnimationKey.CursorType? value)
    {
        element.SetValue(CursorTypeProperty, value);
    }

    public static AssetAnimationKey.CursorType? GetCursorType(Control element)
    {
        return element.GetValue(CursorTypeProperty);
    }

    public static void SetDragImage(TopLevel topLevel, CursorDragImage? value)
    {
        topLevel.SetValue(DragImageProperty, value);
    }

    public static CursorDragImage? GetDragImage(TopLevel topLevel)
    {
        return topLevel.GetValue(DragImageProperty);
    }
}
