using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed class Blankslate : AvaloniaObject
{
    public static readonly AttachedProperty<object?> ContentProperty =
        AvaloniaProperty.RegisterAttached<Blankslate, ItemsControl, object?>("Content");

    static Blankslate()
    {
        ContentProperty.Changed.AddClassHandler<ItemsControl>(OnContentChanged);
    }

    public static void SetContent(ItemsControl element, object? value)
    {
        element.SetValue(ContentProperty, value);
    }

    public static object? GetContent(ItemsControl element)
    {
        return element.GetValue(ContentProperty);
    }

    private static void OnContentChanged(ItemsControl itemsControl, AvaloniaPropertyChangedEventArgs e)
    {
        IPseudoClasses classes = itemsControl.Classes;

        classes.Set(":blankslate", e.NewValue is not null);
    }
}
