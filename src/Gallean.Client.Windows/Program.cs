using Avalonia;

namespace Gallean.Client.Windows;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ClientApp
            .Configure(useDirectRendering: true)
            .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                RenderingMode =
                [
                    Win32RenderingMode.AngleEgl
                ],
                CompositionMode =
                [
                    Win32CompositionMode.LowLatencyDxgiSwapChain
                ],
                OverlayPopups = true
            })
            .AfterSetup(_ =>
            {
                PlatformCursorLock.Install();
                PlatformDpiHandler.Install();
                PlatformBackgroundErase.Install();
            })
            .StartWithClassicDesktopLifetime(args);
    }
}
