using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

public sealed class CursorLock : AvaloniaObject
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<CursorLock, Window, bool>("IsEnabled");

    public static void SetIsEnabled(Window window, bool value)
    {
        window.SetValue(IsEnabledProperty, value);
    }

    public static bool GetIsEnabled(Window window)
    {
        return window.GetValue(IsEnabledProperty);
    }
}
