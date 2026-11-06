using Avalonia.Controls;

namespace Gallean.Client;

/// <summary>
/// A platform full-screen mode used by the game window instead of <see cref="WindowState.FullScreen"/>.
/// </summary>
public interface IFullScreenPresenter
{
    bool IsFullScreen(Window window);

    void SetFullScreen(Window window, bool isFullScreen);
}

public static class FullScreenPresenter
{
    /// <summary>
    /// The platform full-screen mode, or <see langword="null"/> to use <see cref="WindowState.FullScreen"/>.
    /// </summary>
    public static IFullScreenPresenter? Current { get; set; }
}
