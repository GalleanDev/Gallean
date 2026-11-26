using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class CursorScope : AvaloniaObject
{
    [GeneratedAttachedProperty(Inherits = true)]
    public static partial AssetAnimationKey.CursorType? GetCursorType(Control element);

    [GeneratedAttachedProperty]
    public static partial CursorDragImage? GetDragImage(TopLevel topLevel);
}
