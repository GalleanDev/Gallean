using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Gallean.Client;

internal sealed class Backdrop : AvaloniaObject
{
    public static readonly AttachedProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.RegisterAttached<Backdrop, Control, ICommand?>("CloseCommand");

    static Backdrop()
    {
        CloseCommandProperty.Changed.AddClassHandler<Control>(OnCloseCommandChanged);
    }

    public static void SetCloseCommand(Control element, ICommand? value)
    {
        element.SetValue(CloseCommandProperty, value);
    }

    public static ICommand? GetCloseCommand(Control element)
    {
        return element.GetValue(CloseCommandProperty);
    }

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
            e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            ICommand? command = GetCloseCommand(control);

            if (command is not null && command.CanExecute(null))
            {
                command.Execute(null);
                e.Handled = true;
            }
        }
    }
}
