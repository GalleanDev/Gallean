using System.Runtime.InteropServices;

namespace Packer;

internal static class NativeRuntime
{
    public static string Identifier => $"{GetPlatform()}-{GetArchitecture()}";

    private static string GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux";
        }
        else
        {
            throw new PlatformNotSupportedException($"The platform '{RuntimeInformation.OSDescription}' is not supported.");
        }
    }

    private static string GetArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
    }
}
