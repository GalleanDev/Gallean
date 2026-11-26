using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class Blankslate : AvaloniaObject
{
    static Blankslate()
    {
        ContentProperty.Changed.AddClassHandler<ItemsControl>(OnContentChanged);
    }

    [GeneratedAttachedProperty]
    public static partial object? GetContent(ItemsControl element);

    private static void OnContentChanged(ItemsControl itemsControl, AvaloniaPropertyChangedEventArgs e)
    {
        IPseudoClasses classes = itemsControl.Classes;

        classes.Set(":blankslate", e.NewValue is not null);
    }
}
