using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace Gallean.Client.MacOS;

/// <summary>
/// Full screen as a borderless window that covers the whole display, instead of a native full-screen Space.
/// </summary>
/// <remarks>
/// A native full-screen window ends below the camera housing on MacBooks that have one, and macOS does not let it
/// grow into the strip beside the housing. The pointer can move into that strip, where the window gets no pointer
/// events, so the map stops scrolling at the top edge, and the menu bar slides in there. A borderless window can
/// cover the whole display with the menu bar and the Dock hidden. The game is then laid out below the housing by the
/// window padding, while the strip stays part of the window, so the pointer never leaves it.
/// </remarks>
internal sealed class PlatformFullScreen : IFullScreenPresenter
{
    private const nuint PresentationOptions = Api.NSApplicationPresentationHideDock | Api.NSApplicationPresentationHideMenuBar;

    private readonly Dictionary<Window, WindowedState> windowedStates = [];

    public static void Install()
    {
        FullScreenPresenter.Current = new PlatformFullScreen();
        Window.WindowOpenedEvent.AddClassHandler<Window>(OnWindowOpened);
    }

    private static void OnWindowOpened(Window window, RoutedEventArgs e)
    {
        nint nsWindow = GetNSWindow(window);

        if (nsWindow != 0)
        {
            // The green window button would otherwise enter a native full-screen Space, past this mode.
            nuint behavior = Api.SendUInt(nsWindow, Api.Selector("collectionBehavior"));
            Api.Send(nsWindow, Api.Selector("setCollectionBehavior:"), behavior | Api.NSWindowCollectionBehaviorFullScreenNone);
        }
    }

    public bool IsFullScreen(Window window)
    {
        return windowedStates.ContainsKey(window);
    }

    public void SetFullScreen(Window window, bool isFullScreen)
    {
        if (isFullScreen == IsFullScreen(window))
        {
            return;
        }
        else if (isFullScreen)
        {
            Enter(window);
        }
        else
        {
            Exit(window);
        }
    }

    private void Enter(Window window)
    {
        Screen? screen = window.Screens.ScreenFromWindow(window) ?? window.Screens.Primary;

        if (screen is null)
        {
            return;
        }

        nint nsWindow = GetNSWindow(window);

        windowedStates[window] = new WindowedState(
            nsWindow == 0 ? null : Api.SendRect(nsWindow, Api.Selector("frame")),
            window.Width,
            window.Height,
            window.WindowDecorations,
            window.CanResize,
            window.Padding);

        window.Activated += OnWindowActivated;
        window.Closed += OnWindowClosed;

        SetPresentationOptions(PresentationOptions);

        window.WindowDecorations = WindowDecorations.None;
        window.CanResize = false;
        window.Width = screen.Bounds.Width / screen.Scaling;
        window.Height = screen.Bounds.Height / screen.Scaling;
        window.Padding = new Thickness(0, GetTopSafeAreaInset(nsWindow), 0, 0);

        if (nsWindow != 0)
        {
            // AppKit keeps windows positioned through Avalonia below the menu bar, even while it is hidden.
            nint nsScreen = Api.Send(nsWindow, Api.Selector("screen"));

            if (nsScreen != 0)
            {
                Api.Send(nsWindow, Api.Selector("setFrame:display:"), Api.SendRect(nsScreen, Api.Selector("frame")), true);
            }
        }
    }

    private void Exit(Window window)
    {
        if (!windowedStates.Remove(window, out WindowedState? state))
        {
            return;
        }

        window.Activated -= OnWindowActivated;
        window.Closed -= OnWindowClosed;

        SetPresentationOptions(Api.NSApplicationPresentationDefault);

        window.Padding = state.Padding;
        window.WindowDecorations = state.Decorations;
        window.CanResize = state.CanResize;
        window.Width = state.Width;
        window.Height = state.Height;

        nint nsWindow = GetNSWindow(window);

        if (nsWindow != 0 && state.Frame is { } frame)
        {
            // The full-screen frame was set past Avalonia, so the windowed frame is restored the same way.
            Api.Send(nsWindow, Api.Selector("setFrame:display:"), frame, true);
        }
    }

    private static void OnWindowActivated(object? sender, EventArgs e)
    {
        // The options belong to the application; restore them in case anything else changed them.
        SetPresentationOptions(PresentationOptions);
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            Exit(window);
        }
    }

    private static void SetPresentationOptions(nuint options)
    {
        Api.Send(Api.SharedApplication(), Api.Selector("setPresentationOptions:"), options);
    }

    private static nint GetNSWindow(Window window)
    {
        return window.TryGetPlatformHandle() is IMacOSTopLevelPlatformHandle handle ? handle.NSWindow : 0;
    }

    /// <summary>
    /// Gets the height of the camera housing strip at the top of the window's display, or zero if there is none.
    /// </summary>
    private static double GetTopSafeAreaInset(nint nsWindow)
    {
        if (nsWindow == 0)
        {
            return 0;
        }

        nint screen = Api.Send(nsWindow, Api.Selector("screen"));

        // safeAreaInsets is available since macOS 12.
        if (screen == 0 || !Api.SendBool(screen, Api.Selector("respondsToSelector:"), Api.Selector("safeAreaInsets")))
        {
            return 0;
        }

        return Math.Max(Api.SendInsets(screen, Api.Selector("safeAreaInsets")).Top, 0);
    }

    private sealed record WindowedState(
        Api.CGRect? Frame,
        double Width,
        double Height,
        WindowDecorations Decorations,
        bool CanResize,
        Thickness Padding);
}
