using Avalonia;

namespace Gallean.Client.Linux;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ClientApp
            .Configure(useDirectRendering: true)
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                RenderingMode =
                [
                    X11RenderingMode.Glx,
                    X11RenderingMode.Egl
                ],
                OverlayPopups = true
            })
            .StartWithClassicDesktopLifetime(args);
    }
}
