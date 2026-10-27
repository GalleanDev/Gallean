using Avalonia;

namespace Gallean.Client.MacOS;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Metal window surfaces need Avalonia's intermediate surface for compositing effects.
        ClientApp
            .Configure(useDirectRendering: false)
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
    }
}
