using System.Runtime.InteropServices;

namespace Packer;

internal static class ImageMagickNativeLibrary
{
    private const string ImportName = "Magick.Native-Q8-x64.dll";

    public static string FileName
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ImportName;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $"{ImportName}.dylib";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return $"{ImportName}.so";
            }
            else
            {
                throw new PlatformNotSupportedException($"The platform '{RuntimeInformation.OSDescription}' is not supported.");
            }
        }
    }
}
