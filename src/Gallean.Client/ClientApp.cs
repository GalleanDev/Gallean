using Avalonia;
using Gallean.Configuration;

namespace Gallean.Client;

public static class ClientApp
{
    public static AppBuilder Configure(bool useDirectRendering)
    {
        FileLog log = new(AppDirectories.Logs, TimeProvider.System);
        GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();

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
            .Configure(() => new App(log, memoryInfo))
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = GetMaxGpuResourceSizeBytes(memoryInfo)
            });

        if (useDirectRendering)
        {
            builder = builder.WithDirectRendering();
        }

        return builder;
    }

    private static long GetMaxGpuResourceSizeBytes(GCMemoryInfo memoryInfo)
    {
        return Math.Clamp(memoryInfo.TotalAvailableMemoryBytes / 8, 768L * 1024 * 1024, 2L * 1024 * 1024 * 1024);
    }
}
