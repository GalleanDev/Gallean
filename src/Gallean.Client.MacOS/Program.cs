using Avalonia;

namespace Gallean.Client.MacOS;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        ClientApp
            .Configure()
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
    }
}
