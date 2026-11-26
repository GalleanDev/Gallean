using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Gallean.Client;

internal sealed partial class Backdrop : AvaloniaObject
{
    private const double HitMargin = 24;

    static Backdrop()
    {
        CloseCommandProperty.Changed.AddClassHandler<Control>(OnCloseCommandChanged);
    }

    [GeneratedAttachedProperty]
    public static partial ICommand? GetCloseCommand(Control element);

    private static void OnCloseCommandChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        control.PointerPressed -= OnPointerPressed;

        if (e.NewValue is not null)
        {
            control.PointerPressed += OnPointerPressed;
        }
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control &&
            e.Source == control &&
            e.GetCurrentPoint(control).Properties.IsLeftButtonPressed &&
            !HasNearContent(control, e.GetPosition(control)))
        {
            ICommand? command = GetCloseCommand(control);

            if (command is not null && command.CanExecute(null))
            {
                command.Execute(null);
                e.Handled = true;
            }
        }
    }

    private static bool HasNearContent(Control control, Point point)
    {
        for (int row = -1; row <= 1; row++)
        {
            for (int column = -1; column <= 1; column++)
            {
                Point hitPoint = point + new Vector(column * HitMargin, row * HitMargin);
                IInputElement? hitElement = control.InputHitTest(hitPoint);

                if (hitElement is not null && hitElement != control)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
