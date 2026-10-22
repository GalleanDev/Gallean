using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Gallean.Client.Windows;

internal sealed class PlatformCursorLock(Window window)
{
    private Api.RECT? lockedRect;

    public static void Install()
    {
        Window.WindowOpenedEvent.AddClassHandler<Window>(OnWindowOpened);
    }

    private static void OnWindowOpened(Window window, RoutedEventArgs e)
    {
        PlatformCursorLock cursorLock = new(window);
        cursorLock.Attach();
    }

    private void Attach()
    {
        window.Activated += OnWindowChanged;
        window.Deactivated += OnWindowChanged;
        window.PositionChanged += OnWindowChanged;
        window.Resized += OnWindowChanged;
        window.Closed += OnWindowClosed;
        window.PropertyChanged += OnWindowPropertyChanged;
        window.AddHandler(InputElement.PointerMovedEvent, OnWindowChanged, RoutingStrategies.Tunnel, true);
    }

    private void OnWindowChanged(object? sender, EventArgs e)
    {
        Update();
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == CursorLock.IsEnabledProperty)
        {
            Update();
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Release();
    }

    private void Update()
    {
        bool isEnabled = CursorLock.GetIsEnabled(window);

        if (isEnabled &&
            window is { IsActive: true, IsPointerOver: true } &&
            window.WindowState != WindowState.Minimized)
        {
            Lock();
        }
        else
        {
            Release();
        }
    }

    private void Lock()
    {
        Api.RECT rect = GetClientRect();

        if (!Api.GetClipCursor(out Api.RECT current) || current != rect)
        {
            Api.ClipCursor(in rect);
        }

        lockedRect = rect;
    }

    private void Release()
    {
        if (lockedRect is { } rect)
        {
            lockedRect = null;

            if (Api.GetClipCursor(out Api.RECT current) && Contains(rect, current))
            {
                Api.ClipCursor(IntPtr.Zero);
            }
        }
    }

    private Api.RECT GetClientRect()
    {
        PixelPoint topLeft = window.PointToScreen(new Point(0, 0));
        PixelPoint bottomRight = window.PointToScreen(new Point(window.ClientSize.Width, window.ClientSize.Height));

        return new Api.RECT(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
    }

    private static bool Contains(Api.RECT rect, Api.RECT other)
    {
        return other.Left >= rect.Left &&
               other.Top >= rect.Top &&
               other.Right <= rect.Right &&
               other.Bottom <= rect.Bottom;
    }
}
