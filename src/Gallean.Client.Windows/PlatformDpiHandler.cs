using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Gallean.Client.Windows;

internal sealed class PlatformDpiHandler(Window window)
{
    public static void Install()
    {
        Window.WindowOpenedEvent.AddClassHandler<Window>(OnWindowOpened);
    }

    private static void OnWindowOpened(Window window, RoutedEventArgs e)
    {
        PlatformDpiHandler dpiHandler = new(window);
        dpiHandler.Attach();
    }

    private void Attach()
    {
        window.ScalingChanged += OnScalingChanged;
    }

    private void OnScalingChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(Resize);
    }

    private void Resize()
    {
        IPlatformHandle? handle = window.TryGetPlatformHandle();

        if (window.WindowState == WindowState.FullScreen && handle is not null)
        {
            Api.GetClientRect(handle.Handle, out Api.RECT rect);
            Api.SendMessage(handle.Handle, Api.WM_SIZE, Api.SIZE_RESTORED, rect.Right | (rect.Bottom << 16));
        }
    }
}
