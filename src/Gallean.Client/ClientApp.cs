using Avalonia;
using Gallean.Configuration;

namespace Gallean.Client;

public static class ClientApp
{
    private const long MaxGpuResourceSizeBytes = 2L * 1024 * 1024 * 1024;

    public static AppBuilder Configure(bool useDirectRendering)
    {
        FileLog log = new(AppDirectories.Logs, TimeProvider.System);

        AppDomain.CurrentDomain.UnhandledException += (_, ev) =>
        {
            if (ev.ExceptionObject is Exception ex)
            {
                log.Error("Unhandled exception.", ex);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, ev) =>
        {
            log.Error("Unobserved task exception.", ev.Exception);
        };

        AppBuilder builder = AppBuilder
            .Configure(() => new App(log))
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = MaxGpuResourceSizeBytes
            });

        return useDirectRendering ? builder.WithDirectRendering() : builder;
    }
}
