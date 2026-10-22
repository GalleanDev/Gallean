using Avalonia;
using Avalonia.Controls;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed class SettingsScope : AvaloniaObject
{
    public static readonly AttachedProperty<GameSettings> SettingsProperty =
        AvaloniaProperty.RegisterAttached<SettingsScope, Control, GameSettings>("Settings", GameSettings.Default, inherits: true);

    public static void SetSettings(Control element, GameSettings value)
    {
        element.SetValue(SettingsProperty, value);
    }

    public static GameSettings GetSettings(Control element)
    {
        return element.GetValue(SettingsProperty);
    }
}
