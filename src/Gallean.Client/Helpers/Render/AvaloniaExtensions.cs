using Avalonia;
using Avalonia.Platform;

namespace Gallean.Client;

internal static partial class AvaloniaExtensions
{
    public static AppBuilder WithDirectRendering(this AppBuilder builder)
    {
        return builder.AfterPlatformServicesSetup(static _ => InstallDirectRendering());
    }

    private static void InstallDirectRendering()
    {
        IPlatformRenderInterface? platform = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

        if (platform is null)
        {
            throw new InvalidOperationException("No platform render interface is registered.");
        }

        if (platform is not DirectRenderPlatformInterface)
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformRenderInterface>()
                .ToConstant(new DirectRenderPlatformInterface(platform));

            if (AvaloniaLocator.Current.GetService<IPlatformRenderInterface>() is not DirectRenderPlatformInterface)
            {
                throw new InvalidOperationException("Cannot register a platform render interface.");
            }
        }
    }
}
