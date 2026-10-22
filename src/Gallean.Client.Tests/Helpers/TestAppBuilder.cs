using Avalonia;
using Avalonia.Headless;
using Gallean.Client.Tests;
using Gallean.Common;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Gallean.Client.Tests;

internal sealed class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure(() => new App(NullLog.Instance))
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
