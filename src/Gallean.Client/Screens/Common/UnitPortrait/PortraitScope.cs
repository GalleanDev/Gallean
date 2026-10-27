using Avalonia;

namespace Gallean.Client;

internal sealed class PortraitScope : AvaloniaObject
{
    public static readonly AttachedProperty<bool> IsFacingLeftProperty =
        AvaloniaProperty.RegisterAttached<PortraitScope, Visual, bool>("IsFacingLeft", inherits: true);

    public static void SetIsFacingLeft(Visual element, bool value)
    {
        element.SetValue(IsFacingLeftProperty, value);
    }

    public static bool GetIsFacingLeft(Visual element)
    {
        return element.GetValue(IsFacingLeftProperty);
    }
}
