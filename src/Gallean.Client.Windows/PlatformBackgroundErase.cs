using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Gallean.Client.Windows;

internal sealed class PlatformBackgroundErase(Window window)
{
    public static void Install()
    {
        Window.WindowOpenedEvent.AddClassHandler<Window>(OnWindowOpened);
    }

    private static void OnWindowOpened(Window window, RoutedEventArgs e)
    {
        PlatformBackgroundErase backgroundErase = new(window);
        backgroundErase.Attach();
    }

    private void Attach()
    {
        Win32Properties.AddWndProcHookCallback(window, OnWndProc);
    }

    private nint OnWndProc(nint hWnd, uint msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == Api.WM_ERASEBKGND)
        {
            if (window.Background is ISolidColorBrush brush)
            {
                Color color = brush.Color;
                IntPtr gdiBrush = Api.CreateSolidBrush(color.R | ((uint)color.G << 8) | ((uint)color.B << 16));

                Api.GetClientRect(hWnd, out Api.RECT rect);
                Api.FillRect(wParam, rect, gdiBrush);
                Api.DeleteObject(gdiBrush);

                handled = true;
                return 1;
            }
        }

        return 0;
    }
}
